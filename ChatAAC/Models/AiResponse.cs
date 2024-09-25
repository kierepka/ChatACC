using System;

namespace ChatAAC.Models;

public class AiResponse
{
    public string ResponseText { get; set; }
    public DateTime Timestamp { get; set; }

    public AiResponse(string responseText)
    {
        ResponseText = responseText;
        Timestamp = DateTime.Now;
    }
}