using System.Text.Json.Serialization;
using ChatAAC.Converters;

namespace ChatAAC.Models.Obf;

// Class for Sound
public class Sound
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(IntOrStringConverter))]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;

    [JsonPropertyName("data")] public string Data { get; set; } = string.Empty;

    [JsonPropertyName("content_type")] public string ContentType { get; set; } = string.Empty;
}