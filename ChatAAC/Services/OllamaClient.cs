using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ChatAAC.Helpers;
using ChatAAC.Lang;
using ChatAAC.Models;
using ChatAAC.ViewModels;
using OllamaSharp;

namespace ChatAAC.Services;

public class OllamaClient
{
    private readonly Chat _chat;

    public OllamaClient()
    {
        AppLogger.LogInfo(string.Format(Resources.OllamaClient_OllamaClient_Connecting_to__0_____,
	        ConfigViewModel.Instance.OllamaAddress));
        var ollama = new OllamaApiClient(ConfigViewModel.Instance.OllamaAddress)
        {
            SelectedModel = ConfigViewModel.Instance.SelectedModel
        };
        _chat = new Chat(ollama);
    }

    public Task<IAsyncEnumerable<string>> ChatAsync(ChatRequest request)
    {
      
        var prompt =
	        string.Format(Resources.OllamaClientPrompt, 
                request.Prompt, request.Form, request.Tense, request.Quantity,
		        ConfigViewModel.Instance.SelectedLanguage);

        return Task.FromResult(_chat.SendAsync(prompt));
    }
}