namespace Famoria.Application.Models;

public record FetchedEmailData(
    string EmlContent,
    string? ProviderMessageId,
    string? ProviderConversationId,
    string? ProviderSyncToken, // e.g., HistoryId for Gmail
    List<string>? Labels
);
