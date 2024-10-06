using System.Text.Json.Serialization;

namespace ChatAAC.Models.Obf;

// Class for Button
public class Button
{
    [JsonPropertyName("id")] public int Id { get; set; }

    [JsonPropertyName("label")] public string Label { get; set; } = string.Empty;

    [JsonPropertyName("image_id")] public string ImageId { get; set; } = string.Empty;

    [JsonPropertyName("border_color")] public string BorderColor { get; set; } = string.Empty;

    [JsonPropertyName("background_color")] public string BackgroundColor { get; set; } = string.Empty;

    [JsonPropertyName("vocalization")] public string Vocalization { get; set; } = string.Empty;

    [JsonPropertyName("load_board")] public LoadBoard? LoadBoard { get; set; } 

    [JsonPropertyName("action")] public string Action { get; set; } = string.Empty;

    [JsonIgnore] public Image? Image { get; set; }

    private const int ImageWidth = 260;
    private const int ImageHeight = 290;

    [JsonIgnore]
    public int Width
    {
        get
        {
            var width = Image?.Width + 10;
            if (width <= 10) width = 90;
            if (width > ImageWidth) width = ImageWidth;
            return width ?? ImageWidth;
        }
    }

    [JsonIgnore]
    public int Height
    {
        get
        {
            var height = Image?.Height + 30;
            if (height <= 30) height = 110;
            if (height > ImageHeight) height = ImageHeight;
            return height ?? ImageHeight;
        }
    }
}