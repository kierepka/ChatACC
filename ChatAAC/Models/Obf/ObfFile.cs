using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatAAC.Models.Obf;

// Main OBF File class
public class ObfFile
{
    [JsonPropertyName("format")] public string Format { get; set; } = string.Empty;

    [JsonPropertyName("license")] public License License { get; set; } = new();

    [JsonPropertyName("id")] public string Id { get; set; } = string.Empty;

    [JsonPropertyName("locale")] public string Locale { get; set; } = string.Empty;

    [JsonPropertyName("name")] public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description_html")] public string DescriptionHtml { get; set; } = string.Empty;

    [JsonPropertyName("grid")] public Grid? Grid { get; set; } 
    [JsonPropertyName("buttons")] public List<Button> Buttons { get; set; } = new();
    [JsonPropertyName("images")] public List<Image> Images { get; set; } = new();

    [JsonPropertyName("sounds")] public List<Sound> Sounds { get; set; } = new();

    [JsonPropertyName("default_layout")] public string DefaultLayout { get; set; } = string.Empty;

    [JsonPropertyName("url")] public string Url { get; set; } = string.Empty;

    [JsonPropertyName("data_url")] public string DataUrl { get; set; } = string.Empty;

    [JsonPropertyName("ext_coughdrop_settings")]
    public ExtCoughDropSettings ExtCoughDropSettings { get; set; } = new();
}