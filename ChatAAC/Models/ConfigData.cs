using System.Collections.Generic;

namespace ChatAAC.Models;

public class ConfigData
{
    public string OllamaAddress { get; set; } = string.Empty;
    public string SelectedModel { get; set; } = string.Empty;
    public bool ShowSex { get; set; }
    public bool ShowViolence { get; set; }
    public bool ShowAac { get; set; }
    public bool ShowSchematic { get; set; }
    public string? SelectedLanguage { get; set; }
    public int LoadedIconsCount { get; set; }
    public string DefaultBoardPath { get; set; } = string.Empty;
    public double FontSize { get; set; }
    public double ButtonSize { get; set; }
    public List<string>? BoardPaths { get; set; }
}