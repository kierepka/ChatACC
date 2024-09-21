using System.Text.Json.Serialization;

namespace ChatAAC.Models;

public class Keyword
{
    [JsonPropertyName("keyword")]
    public string Text { get; set; }

    [JsonPropertyName("type")]
    public int? Type { get; set; } // Zmiana typu z int na string
}