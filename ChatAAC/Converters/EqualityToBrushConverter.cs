using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace ChatAAC.Converters;

public class EqualityToBrushConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value?.ToString() == parameter?.ToString()
            ? Brushes.Green
            : // Kolor dla wybranego przycisku
            Brushes.Gray; // Kolor dla niewybranego przycisku
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}