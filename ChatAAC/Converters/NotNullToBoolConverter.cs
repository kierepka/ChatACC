using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ChatAAC.Converters;

public class NotNullToBoolConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null; // If it’s not null, return true
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return null; // Typically not used
    }
}