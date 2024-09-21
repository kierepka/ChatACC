using System.Collections.Generic;

namespace ChatAAC.Models;

public class ChatResponse
{
    public List<Choice> Choices { get; set; }
}

public class Choice
{
    public string Text { get; set; }
}