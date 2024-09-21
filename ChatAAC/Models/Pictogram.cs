using System;
using System.Text.Json.Serialization;
using System.IO;

namespace ChatAAC.Models;

public class Pictogram
{
    [JsonPropertyName("_id")]
    public int Id { get; set; }

    [JsonPropertyName("keywords")]
    public Keyword[] Keywords { get; set; }

    // Inne właściwości według potrzeb

    [JsonIgnore]
    public string ImagePath
    {
        get
        {
            var cacheDirectory = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "ChatAAC",
                "Cache",
                "Pictograms");
            return Path.Combine(cacheDirectory, $"{Id}.png");
        }
    }
}