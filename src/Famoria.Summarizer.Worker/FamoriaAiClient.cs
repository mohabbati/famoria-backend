using Famoria.Application.Interfaces;
using Famoria.Application.Models;
using Famoria.Application.Models.Summarizer;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System.Text.Json;

namespace Famoria.Summarizer.Worker;

public sealed class FamoriaAiClient : IFamoriaAiClient
{
    private readonly IChatCompletionService _chat;
    private readonly SummarizerPromptBuilder _promptBuilder;

    private ChatMessageContent? _content;
    private ChatHistory? _history;

    public FamoriaAiClient(IChatCompletionService chat, SummarizerPromptBuilder promptBuilder)
    {
        _chat = chat;
        _promptBuilder = promptBuilder;
    }

    public async Task<AiResponse> GenerateSummaryAsync(ProcessingPrompt prompt, CancellationToken cancellationToken)
    {
        _history = _promptBuilder.BuildChatHistory(prompt);
        _content = await _chat.GetChatMessageContentAsync(_history, cancellationToken: cancellationToken);
        var response = JsonSerializer.Deserialize(
            json: _content.Content!,
            returnType: typeof(AiResponse),
            options: new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }) as AiResponse;

        return response!;
    }

    public Task<string?> ExtractRawResponseAsync()
    {
        ArgumentNullException.ThrowIfNull(_content);

        var result = _content?.Content;
        return Task.FromResult(result);
    }

    public Task<string> ExtractPromptsAsync()
    {
        ArgumentNullException.ThrowIfNull(_history);

        var result = string.Join(Environment.NewLine, _history.Select(x => x.Content));
        return Task.FromResult(result);
    }
}
