using System.Text.Json.Serialization;
using ChatAAC.Converters;

namespace ChatAAC.Models.Obf;

// Class for Button
public class Button
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(IntFromStringConverter))]
    public int? Id { get; set; }

    [JsonPropertyName("label")] public string Label { get; set; } = string.Empty;

    [JsonPropertyName("image_id")] public string ImageId { get; set; } = string.Empty;

    [JsonPropertyName("border_color")] public string BorderColor { get; set; } = string.Empty;

    [JsonPropertyName("background_color")] public string BackgroundColor { get; set; } = string.Empty;

    [JsonPropertyName("vocalization")] public string Vocalization { get; set; } = string.Empty;

    [JsonPropertyName("load_board")] public LoadBoard? LoadBoard { get; set; }

    [JsonPropertyName("action")] public string Action { get; set; } = string.Empty;

    [JsonIgnore] public Image? Image { get; set; }
}