using System.Text.Json.Serialization;
using ChatAAC.Converters;

namespace ChatAAC.Models.Obf;

// Class for Grid (for button layout)
public class Grid
{
    [JsonPropertyName("rows")] public int Rows { get; set; }

    [JsonPropertyName("columns")] public int Columns { get; set; }

    [JsonPropertyName("order")]
    [JsonConverter(typeof(IntOrStringArrayConverter))]
    public string?[][] Order { get; set; } = [];
}