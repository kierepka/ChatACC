using System;
using ReactiveUI;

namespace ChatAAC.Models;

public class AiResponse(string responseText) : ReactiveObject
{
    private string _responseText = responseText;
    private bool _isFavorite;
    private DateTime _timestamp = DateTime.Now;
    
    public string ResponseText
    {
        get => _responseText;
        set => this.RaiseAndSetIfChanged(ref _responseText, value);
    }
   
    public DateTime Timestamp
    {
        get => _timestamp;
        set => this.RaiseAndSetIfChanged(ref _timestamp, value);
    }
    
    
    public bool IsFavorite
    {
        get => _isFavorite;
        set => this.RaiseAndSetIfChanged(ref _isFavorite, value);
    }
}