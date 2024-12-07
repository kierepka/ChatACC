using System.Collections.Generic;

namespace ChatAAC.Models;

public class ChatResponse
{
    public List<Choice> Choices { get; set; } = new();
}