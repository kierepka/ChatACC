using System;
using System.IO;
using ChatAAC.Abstractions.Services;

namespace ChatAAC.Services;

public class CachePathProvider : ICachePathProvider
{
    public string GetObfCacheDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChatAAC", "Cache", "Obf");

    public string GetPictogramsCacheDirectory() => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "ChatAAC", "Cache", "Pictograms");
}