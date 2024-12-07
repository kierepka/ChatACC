// BooleanToClassConverter.cs

using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace ChatAAC.Converters;

public class BooleanToClassConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value != null ? "symbol action" : "symbol";
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}