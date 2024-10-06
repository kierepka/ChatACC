using System.Text.Json.Serialization;

namespace ChatAAC.Models.Obf;

public class Manifest
{
    [JsonPropertyName("format")] public string Format { get; set; } = string.Empty;

    [JsonPropertyName("root")] public string Root { get; set; } = string.Empty;

    [JsonPropertyName("paths")] public Paths Paths { get; set; } = new();
}