using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using ChatAAC.Models;
using ChatAAC.ViewModels;

namespace ChatAAC.Services;

public class AiInteractionService
{
    private readonly OllamaClient _ollamaClient = new();

    /// <summary>
    ///     Sends a constructed sentence to an AI model for processing.
    /// </summary>
    /// <param name="constructedSentence">The sentence to be processed.</param>
    /// <param name="selectedForm">The grammatical form to be used in the response.</param>
    /// <param name="selectedTense">The grammatical tense to be used in the response.</param>
    /// <param name="quantity">The quantity of responses to be generated.</param>
    /// <param name="onError">An action to be invoked in case of an error during communication with the AI.</param>
    /// <returns>
    ///     A task that represents the AI's response as a string. If an error occurs, the task will return an error
    ///     message.
    /// </returns>
    public async Task<string> SendToAiAsync(
        string constructedSentence,
        string selectedForm,
        string selectedTense,
        int quantity,
        Action<string, string> onError)
    {
        if (string.IsNullOrWhiteSpace(constructedSentence)) return "No buttons selected.";

        try
        {
            // Create a chat request
            var chatRequest = new ChatRequest
            {
                Model = ConfigViewModel.Instance.SelectedModel,
                Prompt = constructedSentence,
                Form = selectedForm,
                Tense = selectedTense,
                Quantity = quantity
            };

            var response = await _ollamaClient.ChatAsync(chatRequest).ConfigureAwait(false);
            return await CombineAsyncEnumerableAsync(response).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            onError($"Error communicating with AI: {ex.Message}", "AiInteractionService");
            return $"Error: {ex.Message}";
        }
    }

    /// <summary>
    ///     Combines an asynchronous enumerable of strings into a single string.
    /// </summary>
    /// <param name="asyncStrings">An asynchronous enumerable of strings to be combined.</param>
    /// <returns>A task that represents the combined string.</returns>
    private async Task<string> CombineAsyncEnumerableAsync(IAsyncEnumerable<string> asyncStrings)
    {
        var stringBuilder = new StringBuilder();
        await foreach (var str in asyncStrings.ConfigureAwait(false)) stringBuilder.Append(str);
        return stringBuilder.ToString();
    }
}