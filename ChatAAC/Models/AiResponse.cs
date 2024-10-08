using System;

namespace ChatAAC.Models;

public class AiResponse(string responseText)
{
    public string ResponseText { get; set; } = responseText;
    public DateTime Timestamp { get; set; } = DateTime.Now;
}