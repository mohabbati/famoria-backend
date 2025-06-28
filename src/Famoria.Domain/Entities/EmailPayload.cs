namespace Famoria.Domain.Entities;

// Based on the problem description.
// Includes AttachmentInfo definition.

public class EmailPayload : FamilyItemPayload
{
    public override FamilyItemSource Source => FamilyItemSource.Email;

    public required DateTimeOffset ReceivedAt { get; set; }
    public required string Subject { get; set; }
    public required string SenderName { get; set; }
    public required string SenderEmail { get; set; }

    public List<string>? To { get; set; }
    public List<string>? Cc { get; set; }

    public required string EmlBlobPath { get; set; } // e.g., /{FamilyId}/email/{ItemId}/original.eml
    public List<AttachmentInfo>? Attachments { get; set; } // List of AttachmentInfo

    public string? ProviderMessageId { get; set; }        // Gmail id, Outlook id, etc.
    public string? ProviderConversationId { get; set; }  // Gmail threadId or Outlook conversationId
    public string? ProviderSyncToken { get; set; }       // Gmail historyId, Outlook changeKey, or null

    public List<string>? Labels { get; set; }
}

public record AttachmentInfo(string FileName, string MimeType, long SizeBytes, string BlobPath);
