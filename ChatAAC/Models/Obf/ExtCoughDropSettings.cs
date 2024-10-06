using System.Text.Json.Serialization;

namespace ChatAAC.Models.Obf;

public class ExtCoughDropSettings
{
    [JsonPropertyName("private")]
    public bool Private { get; set; }

    [JsonPropertyName("key")] public string Key { get; set; } = string.Empty;

    [JsonPropertyName("word_suggestions")]
    public bool WordSuggestions { get; set; } 
}