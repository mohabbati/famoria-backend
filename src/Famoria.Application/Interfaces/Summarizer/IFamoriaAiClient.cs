using Famoria.Application.Models;
using Famoria.Application.Models.Summarizer;

namespace Famoria.Application.Interfaces;

public interface IFamoriaAiClient
{
    Task<AiResponse> GenerateSummaryAsync(ProcessingPrompt prompt, CancellationToken cancellationToken);
    Task<string?> ExtractRawResponseAsync();
    Task<string> ExtractPromptsAsync();
}
