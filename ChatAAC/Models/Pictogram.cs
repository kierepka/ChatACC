using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.IO;

namespace ChatAAC.Models;

public class Pictogram
{
    [JsonPropertyName("_id")]
    public int Id { get; set; }

    [JsonPropertyName("keywords")] public Keyword[] Keywords { get; set; } = [];
    
    [JsonPropertyName("image")]
    public string ImageUrl { get; set; } = string.Empty;
    
    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;// Assuming category is a single string
    
    [JsonPropertyName("tags")]
    public List<string> Tags { get; set; } = new List<string>();
    
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