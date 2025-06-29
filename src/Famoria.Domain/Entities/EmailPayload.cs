namespace Famoria.Domain.Entities;

public class EmailPayload : FamilyItemPayload
{
    public override FamilyItemSource Source => FamilyItemSource.Email;

    public DateTimeOffset ReceivedAt { get; set; }
    public string Subject { get; set; } = default!;
    public string SenderName { get; set; } = default!;
    public string SenderEmail { get; set; } = default!;

    public IList<string>? To { get; set; }
    public IList<string>? Cc { get; set; }

    /// <summary>
    /// Blob path to the original <c>.eml</c> file.
    /// Example: <c>/{FamilyId}/email/{ItemId}/original.eml</c>
    /// </summary>
    public string EmlBlobPath { get; set; } = default!;

    public IList<AttachmentInfo>? Attachments { get; set; }

    public string? ProviderMessageId { get; set; }
    public string? ProviderConversationId { get; set; }
    public string? ProviderSyncToken { get; set; }
    public IList<string>? Labels { get; set; }
}

public record AttachmentInfo(string FileName, string MimeType, long SizeBytes, string BlobPath);
