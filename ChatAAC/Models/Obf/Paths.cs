using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatAAC.Models.Obf;

public class Paths
{
    [JsonPropertyName("boards")] public Dictionary<string, string> Boards { get; set; } = new();

    [JsonPropertyName("images")] public Dictionary<string, string> Images { get; set; } = new();

    [JsonPropertyName("sounds")] public Dictionary<string, string> Sounds { get; set; } = new();
}