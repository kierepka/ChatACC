using System;
using System.Globalization;
using System.IO;
using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;

namespace ChatAAC.Converters;

public class Base64ToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string base64String || string.IsNullOrEmpty(base64String))
            return null; // fallback if there's no valid base64
        try
        {
            // Convert the base64 string to a byte array
            var imageBytes = System.Convert.FromBase64String(base64String);

            // Use a MemoryStream to create a Bitmap
            using var ms = new MemoryStream(imageBytes);
            var bmp = new Bitmap(ms);
            return bmp;
        }
        catch
        {
            // If the string is invalid base64, or decoding fails, return null
        }
        return null; // fallback if there's no valid base64
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        // Typically not needed for a one-way binding
        return null;
    }
}