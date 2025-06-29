namespace Famoria.Application.Models;

public record RawEmail(
    string Content,
    string? ProviderMessageId,
    string? ProviderConversationId,
    string? ProviderHistoryId,
    IList<string>? Labels);
