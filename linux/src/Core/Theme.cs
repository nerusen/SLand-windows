using System;
using Avalonia.Media;

namespace Tepi.Linux.Core
{
    public static class TepiTheme
    {
        // 1:1 Palette Tokens extracted from Tone.Dark() in src/Ink.cs
        public static readonly Color ColorBg = Color.FromRgb(0, 0, 0);
        public static readonly Color ColorText = Color.FromRgb(245, 245, 247);
        public static readonly Color ColorSub = Color.FromArgb(158, 245, 245, 247);   // ~62%
        public static readonly Color ColorFaint = Color.FromArgb(92, 245, 245, 247);  // ~36%
        public static readonly Color ColorFill = Color.FromArgb(18, 255, 255, 255);   // ~7%
        public static readonly Color ColorFillHover = Color.FromArgb(28, 255, 255, 255); // ~11%
        public static readonly Color ColorLine = Color.FromArgb(23, 255, 255, 255);   // ~9%
        public static readonly Color ColorHighlight = Color.FromArgb(23, 255, 255, 255);
        public static readonly Color ColorLip = Color.FromArgb(16, 255, 255, 255);
        public static readonly Color ColorAccent = Color.FromRgb(59, 130, 246);      // #3B82F6 (Electric Blue)
        public static readonly Color ColorPositive = Color.FromRgb(110, 200, 140);   // #6EC88C (Mint Green)
        public static readonly Color ColorNegative = Color.FromRgb(240, 120, 120);   // #F07878 (Coral Red)
        public static readonly Color ColorWarning = Color.FromRgb(236, 184, 92);     // #ECB85C (Amber)

        // Avalonia SolidColorBrushes for high-performance rendering
        public static readonly IBrush BrushText = new SolidColorBrush(ColorText);
        public static readonly IBrush BrushSub = new SolidColorBrush(ColorSub);
        public static readonly IBrush BrushFaint = new SolidColorBrush(ColorFaint);
        public static readonly IBrush BrushFill = new SolidColorBrush(ColorFill);
        public static readonly IBrush BrushFillHover = new SolidColorBrush(ColorFillHover);
        public static readonly IBrush BrushLine = new SolidColorBrush(ColorLine);
        public static readonly IBrush BrushAccent = new SolidColorBrush(ColorAccent);
        public static readonly IBrush BrushPositive = new SolidColorBrush(ColorPositive);
        public static readonly IBrush BrushNegative = new SolidColorBrush(ColorNegative);
        public static readonly IBrush BrushWarning = new SolidColorBrush(ColorWarning);

        // PRoot Dark Acrylic Surfaces (zero-CPU blur alternative to Liquid Glass)
        public static readonly Color HullDarkTop = Color.FromArgb(238, 18, 18, 23);    // #EE121217 (~93% alpha)
        public static readonly Color HullDarkBottom = Color.FromArgb(238, 10, 10, 14); // #EE0A0A0E

        public static readonly IBrush HullBackground = new LinearGradientBrush
        {
            StartPoint = new Avalonia.RelativePoint(0.5, 0, Avalonia.RelativeUnit.Relative),
            EndPoint = new Avalonia.RelativePoint(0.5, 1, Avalonia.RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(HullDarkTop, 0),
                new GradientStop(HullDarkBottom, 1)
            }
        };

        // Double Specular Stroke (top glass reflection fading to bottom edge)
        public static readonly IBrush HullBorderStroke = new LinearGradientBrush
        {
            StartPoint = new Avalonia.RelativePoint(0.5, 0, Avalonia.RelativeUnit.Relative),
            EndPoint = new Avalonia.RelativePoint(0.5, 1, Avalonia.RelativeUnit.Relative),
            GradientStops = new GradientStops
            {
                new GradientStop(Color.FromArgb(46, 255, 255, 255), 0),   // 18% white highlight
                new GradientStop(Color.FromArgb(10, 255, 255, 255), 1)    // 4% white baseline
            }
        };

        // Solid Opaque Mode (for VNC environments without picom/compositor)
        public static readonly IBrush OpaqueBackground = new SolidColorBrush(Color.FromRgb(16, 16, 20));
        public static readonly IBrush OpaqueBorder = new SolidColorBrush(Color.FromArgb(64, 255, 255, 255));

        // Elevated Card Surface (for inner panels & cards)
        public static readonly IBrush CardBackground = new SolidColorBrush(Color.FromRgb(22, 22, 30));
        public static readonly IBrush CardBorder = new SolidColorBrush(Color.FromRgb(32, 32, 42));
    }
}
