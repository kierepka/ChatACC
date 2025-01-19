using System.Text.Json.Serialization;
using ChatAAC.Converters;

namespace ChatAAC.Models.Obf;

// Klasa reprezentująca przycisk
public class Button
{
    [JsonPropertyName("id")]
    [JsonConverter(typeof(IntOrStringConverter))]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("label")] public string Label { get; set; } = string.Empty;

    [JsonPropertyName("image_id")]
    [JsonConverter(typeof(IntOrStringConverter))]
    public string ImageId { get; set; } = string.Empty;

    [JsonPropertyName("border_color")] public string BorderColor { get; set; } = string.Empty;

    [JsonPropertyName("background_color")] public string BackgroundColor { get; set; } = string.Empty;

    [JsonPropertyName("vocalization")] public string Vocalization { get; set; } = string.Empty;

    [JsonPropertyName("load_board")] public LoadBoard? LoadBoard { get; set; }

    [JsonPropertyName("action")] public string Action { get; set; } = string.Empty;

    [JsonIgnore] public Image? Image { get; set; }
}