using System.Text.Json;

using Famoria.Application.Models;

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace Famoria.Summarizer.Worker;

/// <summary>
/// Builds ChatHistory for the Famoria summarisation task.
/// No dependency on SK plugins; purely a helper class => **type‑safe**.
/// </summary>
public sealed class SummarizerPromptBuilder
{
    private static readonly string SystemPrompt = File.ReadAllText(
        Path.Combine(AppContext.BaseDirectory + @"\Plugins\SummarizeFamilyItem", "sk-prompt.txt"));

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public ChatHistory BuildChatHistory(ProcessingPrompt input)
    {
        ArgumentNullException.ThrowIfNull(input);

        var history = new ChatHistory();
        history.AddSystemMessage(SystemPrompt);
        history.AddUserMessage(JsonSerializer.Serialize(input, JsonOptions));
        return history;
    }
}
