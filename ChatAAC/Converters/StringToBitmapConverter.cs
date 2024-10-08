using Avalonia.Data.Converters;
using Avalonia.Media.Imaging;
using System;
using System.Globalization;
using System.IO;
using SkiaSharp;
using Svg.Skia;

namespace ChatAAC.Converters;

public class StringToBitmapConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not string path || !File.Exists(path)) return null;
        try
        {
            // Zakładając, że ścieżka wskazuje na plik SVG
            if (!path.EndsWith(".svg", StringComparison.OrdinalIgnoreCase)) return new Bitmap(path);
            using var stream = File.OpenRead(path);

            using var svg = new SKSvg();
            svg.Load(stream);

            if (svg.Picture == null) return null;
            
            // Ustawienie docelowego rozmiaru obrazu
            const int targetWidth = 250;
            const int targetHeight = 250;

            // Obliczanie skali, aby zachować proporcje obrazu
            float scaleX = targetWidth / svg.Picture.CullRect.Width;
            float scaleY = targetHeight / svg.Picture.CullRect.Height;
            float scale = Math.Min(scaleX, scaleY);

            // Tworzenie i skalowanie bitmapy
            var scaledSize = new SKImageInfo(targetWidth, targetHeight);
            using var bitmap = new SKBitmap(scaledSize);
            using var canvas = new SKCanvas(bitmap);
            canvas.Clear(SKColors.Transparent);

            var matrix = SKMatrix.CreateScale(scale, scale);
            canvas.DrawPicture(svg.Picture, ref matrix);
            canvas.Flush();

            // Konwersja SKBitmap na Bitmapę Avalonii
            using var image = SKImage.FromBitmap(bitmap);
            using var data = image.Encode(SKEncodedImageFormat.Png, 100);
            using var ms = new MemoryStream();
            data.SaveTo(ms);
            ms.Seek(0, SeekOrigin.Begin);
            return new Bitmap(ms);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Błąd podczas tworzenia Bitmap z SVG: {ex.Message}");
            return null;
        }
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}