namespace Famoria.Application.Interfaces;

/// <summary>
/// Defines a client for sending prompts to an LLM and receiving raw JSON responses.
/// </summary>
public interface IFamoriaAiClient
{
    /// <summary>
    /// Sends a serialized prompt JSON string to the configured AI provider and returns the raw JSON response.
    /// </summary>
    /// <param name="promptJson">The JSON-serialized prompt payload.</param>
    /// <param name="cancellationToken">A cancellation token.</param>
    /// <returns>The raw JSON response from the LLM.</returns>
    Task<string> GenerateSummaryAsync(string promptJson, CancellationToken cancellationToken);
}
