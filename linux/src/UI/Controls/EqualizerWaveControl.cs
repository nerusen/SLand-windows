using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Tepi.Linux.Core;

namespace Tepi.Linux.UI.Controls
{
    public class EqualizerWaveControl : Control
    {
        private const int BarCount = 5;
        private const float BarWidth = 3f;
        private const float BarGap = 2.4f;
        private const float MaxWaveHeight = 16f;

        private readonly float[] _heights = new float[BarCount];
        private readonly float[] _targets = new float[BarCount];
        private readonly Random _random = new();
        private readonly DispatcherTimer _timer;

        private bool _isPlaying;

        public EqualizerWaveControl()
        {
            Width = BarCount * (BarWidth + BarGap) - BarGap;
            Height = MaxWaveHeight;

            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(70) }; // ~14 FPS: very light on ARM
            _timer.Tick += (s, e) =>
            {
                if (!_isPlaying)
                {
                    _timer.Stop();
                    InvalidateVisual();
                    return;
                }

                for (int i = 0; i < BarCount; i++)
                {
                    _targets[i] = 3f + (float)_random.NextDouble() * (MaxWaveHeight - 3f);
                    _heights[i] = _heights[i] + (_targets[i] - _heights[i]) * 0.45f;
                }
                InvalidateVisual();
            };
        }

        public bool IsPlaying
        {
            get => _isPlaying;
            set
            {
                if (_isPlaying != value)
                {
                    _isPlaying = value;
                    if (_isPlaying)
                    {
                        _timer.Start();
                    }
                    else
                    {
                        for (int i = 0; i < BarCount; i++) _heights[i] = 4f;
                        InvalidateVisual();
                    }
                }
            }
        }

        public override void Render(DrawingContext context)
        {
            base.Render(context);

            float cy = (float)Bounds.Height / 2f;
            float x = 0f;

            for (int i = 0; i < BarCount; i++)
            {
                float h = _isPlaying ? _heights[i] : 3.5f;
                float top = cy - h / 2f;
                var rect = new Rect(x, top, BarWidth, h);
                context.DrawRectangle(TepiTheme.BrushText, null, new RoundedRect(rect, 1.5));
                x += BarWidth + BarGap;
            }
        }
    }
}
