using System;
using System.Globalization;
using System.Linq;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ChatAAC.Converters;

public class ColorConverter : IValueConverter
{
    
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string colorString) return Brushes.Transparent;
        if (string.IsNullOrEmpty(colorString)) return Colors.Transparent;
        Color color;
        
        try
        {
            // Avalonia supports Color.Parse for hex or named colors
            color = Color.Parse(colorString);
        }
        catch
        {
            color =  Colors.Transparent;
        }
        
        try
        {
            var rgb = colorString.Split(['(', ',', ')'], StringSplitOptions.RemoveEmptyEntries)
                .Skip(1) // Skip "rgb"
                .Select(int.Parse).ToList();
            color = Color.FromArgb(255, (byte)rgb[0], (byte)rgb[1], (byte)rgb[2]);
        }
        catch
        {
            color =  Colors.Transparent;
        }
        
        return new SolidColorBrush(color);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // If you need two-way binding, parse the brush back to a string. 
        // Otherwise, just return null for one-way.
        if (value is SolidColorBrush brush)
            return brush.Color.ToString();
        return null;
    }

    
}