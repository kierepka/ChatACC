using System.Text.Json.Serialization;
using ChatAAC.Converters;

namespace ChatAAC.Models.Obf;

// Class for Image
public class Image
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(IntOrStringConverter))]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;

    [JsonPropertyName("data_url")] public string DataUrl { get; set; } = string.Empty;
    [JsonPropertyName("data")] public string Data { get; set; } = string.Empty;

    [JsonPropertyName("content_type")] public string ContentType { get; set; } = string.Empty;

    [JsonPropertyName("width")] public int Width { get; set; }

    [JsonPropertyName("height")] public int Height { get; set; }

    [JsonPropertyName("license")] public License License { get; set; } = new();

    [JsonPropertyName("path")] public string Path { get; set; } = string.Empty;

    [JsonIgnore] public string ImagePath { get; set; } = string.Empty;
}