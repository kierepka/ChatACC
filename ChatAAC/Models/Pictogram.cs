using System;
using System.IO;
using System.Text.Json.Serialization;

namespace ChatAAC.Models;

public class Pictogram
{
    [JsonPropertyName("schematic")]
    public bool Schematic { get; set; } = false;

    [JsonPropertyName("sex")]
    public bool Sex { get; set; } = false;

    [JsonPropertyName("violence")]
    public bool Violence { get; set; } = false;

    [JsonPropertyName("aac")]
    public bool Aac { get; set; } = false;

    [JsonPropertyName("aacColor")]
    public bool AacColor { get; set; } = false;

    [JsonPropertyName("skin")]
    public bool Skin { get; set; } = false;

    [JsonPropertyName("hair")]
    public bool Hair { get; set; } = false;

    [JsonPropertyName("downloads")]
    public long Downloads { get; set; } = 0;

    [JsonPropertyName("categories")]
    public string[] Categories { get; set; } = Array.Empty<string>();

    [JsonPropertyName("synsets")]
    public string[] Synsets { get; set; } = Array.Empty<string>();

    [JsonPropertyName("tags")]
    public string[] Tags { get; set; } = Array.Empty<string>();

    [JsonPropertyName("_id")]
    public long Id { get; set; } = 0;

    [JsonPropertyName("created")]
    public DateTimeOffset Created { get; set; }

    [JsonPropertyName("lastUpdated")]
    public DateTimeOffset LastUpdated { get; set; }

    [JsonPropertyName("keywords")]
    public Keyword[] Keywords { get; set; } = Array.Empty<Keyword>();

    // Publiczny, bezparametrowy konstruktor
    public Pictogram() { }

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