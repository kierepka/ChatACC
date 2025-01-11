using System.Text.Json.Serialization;
using ChatAAC.Converters;

namespace ChatAAC.Models.Obf;

// Class for LoadBoard (button linked to another board)
public class LoadBoard
{
    [JsonPropertyName("id")] 
    [JsonConverter(typeof(IntOrStringConverter))]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("data_url")] public string DataUrl { get; set; } = string.Empty;

    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;

    [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;
}