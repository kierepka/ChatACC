namespace ChatAAC.Models;

public class ChatRequest
{
    public string Prompt { get; set; } = string.Empty;

    public string Form { get; set; } = string.Empty;

    public string Tense { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;

    public int Quantity { get; set; }
}