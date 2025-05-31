namespace Famoria.Domain.Entities;

public class EmailPayload : FamilyItemPayload
{
    public override FamilyItemSource Source => FamilyItemSource.Email;
    public required DateTime ReceivedAt { get; set; }
    public required string Subject { get; set; }
    public required string Sender { get; set; }
    public required string EmlBlobPath { get; set; } // e.g., /{FamilyId}/{ItemId}/original.eml
    public List<string>? AttachmentBlobPaths { get; set; } // /{FamilyId}/{ItemId}/attachments/{filename.ext}
}
