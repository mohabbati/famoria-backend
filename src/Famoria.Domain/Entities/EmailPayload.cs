namespace Famoria.Domain.Entities;

public class EmailPayload : FamilyItemPayload
{
    public override FamilyItemSource Source => FamilyItemSource.Email;
    public required DateTime ReceivedAt { get; set; }
    public required string Subject { get; set; }
    public required string Sender { get; set; }
    public string? HtmlBody { get; set; }
    public string? TextBody { get; set; }
    public List<string>? Attachments { get; set; }
}
