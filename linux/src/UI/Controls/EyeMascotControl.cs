using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Tepi.Linux.Core;

namespace Tepi.Linux.UI.Controls
{
    public enum EyeMood
    {
        None,
        Happy,
        Surprised,
        Sleepy,
        Glance
    }

    public class EyeMascotControl : Control
    {
        // Vector dimensions exact from src/Eyes.cs
        private const float EyeW = 6f;
        private const float EyeH = 9f;
        private const float EyeGap = 9.5f;
        private const float EyeReachX = 3f;
        private const float EyeReachY = 1.5f;

        // Springs from audit
        private readonly Spring _lookX = Spring.ForGaze(0);
        private readonly Spring _lookY = Spring.ForGaze(0);
        private readonly Spring _eyeLid = Spring.ForEyelid(0);

        private readonly Random _random = new();
        private readonly DispatcherTimer _frameTimer;
        private readonly DispatcherTimer _blinkScheduler;

        private EyeMood _mood = EyeMood.None;
        private DateTime _blinkStartUtc = DateTime.MinValue;
        private DateTime _lastFrameUtc = DateTime.UtcNow;
        private bool _blinkTwice;
        private bool _isAnimating;

        private static Pen? _smilingEyePen;
        private static Pen SmilingEyePen => _smilingEyePen ??= new Pen(TepiTheme.BrushText, 1.8)
        {
            LineCap = PenLineCap.Round,
            LineJoin = PenLineJoin.Round
        };

        public EyeMascotControl()
        {
            Width = 32;
            Height = 22;

            _frameTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(33) }; // ~30 FPS max
            _frameTimer.Tick += OnFrameTick;

            _blinkScheduler = new DispatcherTimer();
            _blinkScheduler.Tick += (s, e) =>
            {
                _blinkScheduler.Stop();
                TriggerBlink();
            };

            ScheduleNextBlink();
        }

        public EyeMood Mood
        {
            get => _mood;
            set
            {
                if (_mood != value)
                {
                    _mood = value;
                    WakeAnimation();
                }
            }
        }

        // Pointer tracking called from IslandWindow.PointerMoved
        public void TrackPointer(Point pointerRelativeToCenter)
        {
            float dx = (float)pointerRelativeToCenter.X;
            float dy = (float)pointerRelativeToCenter.Y;

            // Follow formula from src/Eyes.cs
            float tx = EyeReachX * dx / (Math.Abs(dx) + 80f);
            float ty = EyeReachY * dy / (Math.Abs(dy) + 50f);

            _lookX.Target = Math.Clamp(tx, -EyeReachX, EyeReachX);
            _lookY.Target = Math.Clamp(ty, -EyeReachY, EyeReachY);

            WakeAnimation();
        }

        public void ResetGaze()
        {
            _lookX.Target = 0;
            _lookY.Target = 0;
            WakeAnimation();
        }

        public void TriggerBlink()
        {
            _blinkStartUtc = DateTime.UtcNow;
            _blinkTwice = _random.NextDouble() < 0.18; // 18% double blink chance from src/Eyes.cs
            WakeAnimation();
        }

        private void ScheduleNextBlink()
        {
            double seconds = 2.5 + _random.NextDouble() * 3.5; // Every 2.5 - 6s
            _blinkScheduler.Interval = TimeSpan.FromSeconds(seconds);
            _blinkScheduler.Start();
        }

        private void WakeAnimation()
        {
            if (!_isAnimating)
            {
                _isAnimating = true;
                _lastFrameUtc = DateTime.UtcNow;
                _frameTimer.Start();
            }
            InvalidateVisual();
        }

        private void OnFrameTick(object? sender, EventArgs e)
        {
            var now = DateTime.UtcNow;
            float dt = (float)Math.Min(0.05, (now - _lastFrameUtc).TotalSeconds);
            _lastFrameUtc = now;

            bool movingX = _lookX.Step(dt);
            bool movingY = _lookY.Step(dt);
            bool movingLid = _eyeLid.Step(dt);

            float close = CalculateBlinkClose(now);
            bool still = !movingX && !movingY && !movingLid && close <= 0;

            InvalidateVisual();

            if (still)
            {
                _isAnimating = false;
                _frameTimer.Stop(); // Guarantee 0 FPS at rest!
                ScheduleNextBlink();
            }
        }

        private float CalculateBlinkClose(DateTime now)
        {
            if (_blinkStartUtc == DateTime.MinValue) return 0f;

            float shut = 0.07f;
            float hold = 0.04f;
            float open = 0.12f;
            float one = shut + hold + open;
            float gap = 0.09f;

            double t = (now - _blinkStartUtc).TotalSeconds;
            if (t < 0) return 0f;

            double totalCycle = _blinkTwice ? (one * 2 + gap) : one;
            if (t >= totalCycle)
            {
                _blinkStartUtc = DateTime.MinValue;
                return 0f;
            }

            if (_blinkTwice && t >= one)
            {
                t -= (one + gap);
                if (t < 0) return 0f;
            }

            if (t < shut)
            {
                float u = (float)(t / shut);
                return u * u * u;
            }
            if (t < shut + hold) return 1f;

            float v = 1f - (float)((t - shut - hold) / open);
            return Math.Clamp(v * v * v, 0f, 1f);
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            float cx = (float)Bounds.Width / 2f + _lookX.Value;
            float cy = (float)Bounds.Height / 2f + _lookY.Value;

            float close = CalculateBlinkClose(DateTime.UtcNow);
            float happy = _mood == EyeMood.Happy ? 1f : 0f;
            float wow = _mood == EyeMood.Surprised ? 1f : 0f;
            float lid = _mood == EyeMood.Sleepy ? 0.7f : 0f;

            float halfSpan = (EyeGap + EyeW) / 2f;

            // Render both eyes symmetrically with parallax
            PaintSingleEye(context, cx - halfSpan, cy, close, happy, wow, lid);
            PaintSingleEye(context, cx + halfSpan, cy, close, happy, wow, lid);
        }

        private void PaintSingleEye(DrawingContext context, float x, float cy, float close, float happy, float wow, float lid)
        {
            float w = EyeW * (1f + 0.27f * wow);
            float h = EyeH * (1f + 0.16f * wow);

            // Smiling Eye Expression (Music or Done)
            if (happy > 0.01f)
            {
                float rw = w * 0.7f;
                var geom = new StreamGeometry();
                using (var ctx = geom.Open())
                {
                    ctx.BeginFigure(new Point(x - rw, cy + 1.2), false);
                    ctx.ArcTo(new Point(x + rw, cy + 1.2), new Size(rw, rw * 0.75), 0, false, SweepDirection.Clockwise);
                }
                context.DrawGeometry(null, SmilingEyePen, geom);
                return;
            }

            // Blink squashes vertical height
            float hv = Math.Max(0.7f, h * (1f - 0.92f * close));
            float top = cy - h / 2f + (h - hv) * 0.62f;
            float left = x - w / 2f;

            float radius = Math.Min(w, hv) / 2f;

            if (lid > 0.1f)
            {
                // Sleepy drooped eyelid
                float lidY = cy - h / 2f + lid * h * 0.45f;
                var rect = new Rect(left, Math.Max(top, lidY), w, Math.Max(1.0, top + hv - lidY));
                context.DrawRectangle(TepiTheme.BrushText, null, new RoundedRect(rect, radius, radius, radius, radius));
            }
            else
            {
                // Standard upright capsule pill
                var rect = new Rect(left, top, w, hv);
                context.DrawRectangle(TepiTheme.BrushText, null, new RoundedRect(rect, radius));
            }
        }
    }
}
