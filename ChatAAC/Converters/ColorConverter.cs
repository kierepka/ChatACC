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
        if (value is not string colorString) return Colors.Transparent;
        
        if (string.IsNullOrEmpty(colorString)) return Colors.Transparent;
        
        var rgb = colorString.Split(['(', ',', ')'], StringSplitOptions.RemoveEmptyEntries)
            .Skip(1) // Skip "rgb"
            .Select(int.Parse).ToList();
        return Color.FromArgb(255, (byte)rgb[0], (byte)rgb[1], (byte)rgb[2]);
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}