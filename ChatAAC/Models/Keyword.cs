using System.Text.Json.Serialization;

namespace ChatAAC.Models;

public class Keyword
{
    [JsonPropertyName("type")] public long? Type { get; set; }

    [JsonPropertyName("keyword")] public string KeywordKeyword { get; set; } = string.Empty;

    [JsonPropertyName("hasLocution")] public bool HasLocution { get; set; }

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("plural")]
    public string Plural { get; set; } = string.Empty;

    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    [JsonPropertyName("meaning")]
    public string Meaning { get; set; } = string.Empty;
}