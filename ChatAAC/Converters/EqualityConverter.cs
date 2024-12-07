using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace ChatAAC.Converters;

public class EqualityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return Equals(value?.ToString(), parameter?.ToString());
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value is bool and true ? parameter : AvaloniaProperty.UnsetValue;
    }
}