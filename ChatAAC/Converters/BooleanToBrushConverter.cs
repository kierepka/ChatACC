using System;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ChatAAC.Converters;

public class BooleanToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        return value is bool and true ? Brushes.LightYellow : // Highlight favorites
            Brushes.Transparent;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotSupportedException();
    }
}