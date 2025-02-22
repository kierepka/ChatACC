using System;
using System.IO;
using System.Linq;
using ChatAAC.Abstractions.Services;

namespace ChatAAC.Services;

public class FileTypeValidator : IFileTypeValidator
{
    public bool IsImageFile(string fileName)
    {
        var extensions = new[] { ".jpg", ".jpeg", ".png", ".bmp" };
        var fileExtension = Path.GetExtension(fileName).ToLowerInvariant();
        return extensions.Contains(fileExtension);
    }

    public bool IsObfFile(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() == ".obf";
    }

    public bool IsManifestFile(string fileName)
    {
        return fileName.Equals("manifest.json", StringComparison.OrdinalIgnoreCase);
    }

    public bool IsObzFile(string fileName)
    {
        return Path.GetExtension(fileName).ToLowerInvariant() == ".obz";
    }
}