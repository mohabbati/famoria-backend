using Azure.Storage.Blobs;
using Famoria.Application.Models;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Text;

namespace Famoria.Application.Services;

public class EmailPersistenceService : IEmailPersistenceService
{
    private readonly BlobContainerClient _blobContainerClient;
    private readonly ICosmosRepository<FamilyItem> _repository;
    private readonly ILogger<EmailPersistenceService> _logger;

    public EmailPersistenceService(
        BlobContainerClient blobContainerClient,
        ICosmosRepository<FamilyItem> repository,
        ILogger<EmailPersistenceService> logger)
    {
        _logger = logger;
        _repository = repository;
        _blobContainerClient = blobContainerClient;
    }

    public async Task<string> PersistAsync(RawEmail rawEmail, string familyId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var itemId = IdFactory.NewGuidId();
        try
        {
            var mime = await MimeMessage.LoadAsync(new MemoryStream(Encoding.UTF8.GetBytes(rawEmail.Content)), cancellationToken);

            var subject = mime.Subject ?? string.Empty;
            var from = mime.From.Mailboxes.FirstOrDefault();
            var senderName = from?.Name ?? string.Empty;
            var senderEmail = from?.Address ?? string.Empty;
            var receivedAt = mime.Date;

            // Upload original .eml
            var emlBlobPath = $"{familyId}/email/{itemId}/original.eml";
            var emlBlobClient = _blobContainerClient.GetBlobClient(emlBlobPath);
            await emlBlobClient.UploadAsync(new BinaryData(rawEmail.Content), overwrite: true, cancellationToken).ConfigureAwait(false);

            // Upload attachments (including inline images)
            var attachments = new List<AttachmentInfo>();
            foreach (var part in mime.BodyParts.OfType<MimePart>())
            {
                if (!string.IsNullOrEmpty(part.FileName))
                {
                    var fileName = part.FileName ?? IdFactory.NewGuidId();
                    var attachmentBlobPath = $"{familyId}/email/{itemId}/attachments/{fileName}";
                    var attachmentBlobClient = _blobContainerClient.GetBlobClient(attachmentBlobPath);
                    await using var stream = new MemoryStream();
                    await part.Content.DecodeToAsync(stream, cancellationToken).ConfigureAwait(false);
                    var size = stream.Length;
                    stream.Position = 0;
                    await attachmentBlobClient.UploadAsync(stream, overwrite: true, cancellationToken).ConfigureAwait(false);
                    attachments.Add(new AttachmentInfo(fileName, part.ContentType.MimeType, size, attachmentBlobPath));
                }
            }

            // Create FamilyItem
            var payload = new EmailPayload
            {
                ReceivedAt = receivedAt,
                Subject = subject,
                SenderName = senderName,
                SenderEmail = senderEmail,
                EmlBlobPath = emlBlobPath,
                Attachments = attachments.Count > 0 ? attachments : null,
                ProviderMessageId = rawEmail.ProviderMessageId,
                ProviderConversationId = rawEmail.ProviderConversationId,
                ProviderSyncToken = rawEmail.ProviderHistoryId,
                Labels = rawEmail.Labels
            };
            var familyItem = new FamilyItem
            {
                Id = itemId,
                FamilyId = familyId,
                Source = FamilyItemSource.Email,
                Payload = payload,
                Status = FamilyItemStatus.New
            };

            // Persist to Cosmos DB
            await _repository.AddAsync(familyItem, cancellationToken);

            return itemId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist email for family {FamilyId}: {Message}", familyId, ex.Message);
            throw;
        }
    }
}
