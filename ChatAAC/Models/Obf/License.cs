using System.Text.Json.Serialization;

namespace ChatAAC.Models.Obf;

public class License
{
    [JsonPropertyName("type")] public string Type { get; set; } = string.Empty;

    [JsonPropertyName("copyright_notice_url")]
    public string CopyrightNoticeUrl { get; set; } = string.Empty;

    [JsonPropertyName("author_name")] public string AuthorName { get; set; } = string.Empty;

    [JsonPropertyName("author_url")] public string AuthorUrl { get; set; } = string.Empty;
}