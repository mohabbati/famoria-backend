namespace Famoria.Domain.Entities;

public class EmailPayload : FamilyItemPayload
{
    public override FamilyItemSource Source => FamilyItemSource.Email;

    public required DateTimeOffset ReceivedAt { get; set; }
    public required string Subject { get; set; }
    public required string SenderName { get; set; }
    public required string SenderEmail { get; set; }

    public List<string>? To { get; set; }
    public List<string>? Cc { get; set; }

    /// <summary>
    /// Blob path to the original <c>.eml</c> file.
    /// Example: <c>/{FamilyId}/email/{ItemId}/original.eml</c>
    /// </summary>
    public required string EmlBlobPath { get; set; }

    public List<AttachmentInfo>? Attachments { get; set; }

    public string? ProviderMessageId { get; set; }
    public string? ProviderConversationId { get; set; }
    public string? ProviderSyncToken { get; set; }
    public List<string>? Labels { get; set; }
}

public record AttachmentInfo(string FileName, string MimeType, long SizeBytes, string BlobPath);
