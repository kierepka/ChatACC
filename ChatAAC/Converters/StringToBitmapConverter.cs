using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;
using System.IO;

namespace ChatAAC.Converters;

public class StringToBitmapConverter : IValueConverter
{
    public static StringToBitmapConverter Instance = new StringToBitmapConverter();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || !File.Exists(path)) return null;
        try
        {
            return new Bitmap(path);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas tworzenia Bitmap z {path}: {ex.Message}");
            // Możesz zwrócić domyślny obraz lub null
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}