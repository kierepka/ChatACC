using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ChatAAC.Converters
{
    /// <summary>
    /// Returns a brush with a contrasting color (white or black) 
    /// compared to the given background color.
    /// </summary>
    public class ContrastForegroundBrushConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not SolidColorBrush backgroundBrush) return Brushes.Black; // fallback
            var color = backgroundBrush.Color;

            // Compute luminance (0..1)
            var luminance = ComputeRelativeLuminance(color);

            // If the background is dark, return White, else Black
            return luminance < 0.5 ? Brushes.White : Brushes.Black;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // Usually one-way binding, so no need to convert back
            return null!;
        }

        private static double ComputeRelativeLuminance(Color color)
        {
            // Standard formula for relative luminance (from W3C):
            // https://www.w3.org/TR/WCAG20/#relativeluminancedef
            // R, G, B are in [0..1].
            var r = color.R / 255.0;
            var g = color.G / 255.0;
            var b = color.B / 255.0;

            // Apply gamma correction
            r = (r <= 0.03928) ? (r / 12.92) : Math.Pow((r + 0.055) / 1.055, 2.4);
            g = (g <= 0.03928) ? (g / 12.92) : Math.Pow((g + 0.055) / 1.055, 2.4);
            b = (b <= 0.03928) ? (b / 12.92) : Math.Pow((b + 0.055) / 1.055, 2.4);

            // 0.2126 R + 0.7152 G + 0.0722 B
            return 0.2126 * r + 0.7152 * g + 0.0722 * b;
        }
    }
}