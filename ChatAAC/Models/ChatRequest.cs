namespace ChatAAC.Models;

public class ChatRequest
{
    public string Prompt { get; set; } = string.Empty;

    public string Model { get; set; } = string.Empty;
    // Dodaj inne właściwości zgodnie z wymaganiami OllamaSharp
}