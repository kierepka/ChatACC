using Avalonia.Data.Converters;
using System;
using System.Globalization;

namespace ChatAAC.Converters
{
    public class StringNotEmptyToBoolConverter : IValueConverter
    {
        public static StringNotEmptyToBoolConverter Instance { get; } = new StringNotEmptyToBoolConverter();

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return !string.IsNullOrEmpty(value as string);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}