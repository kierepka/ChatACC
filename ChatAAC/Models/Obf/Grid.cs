using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace ChatAAC.Models.Obf;
// Class for Grid (for button layout)
public class Grid
{
    [JsonPropertyName("rows")] public int Rows { get; set; }

    [JsonPropertyName("columns")] public int Columns { get; set; }

    [JsonPropertyName("order")]  public string?[][] Order { get; set; } = [];
}