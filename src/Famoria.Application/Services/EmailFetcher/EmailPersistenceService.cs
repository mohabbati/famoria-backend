using Azure.Storage.Blobs;
using Famoria.Domain.Common;
using Microsoft.Extensions.Logging;
using System.Text;
using MimeKit;
using Famoria.Application.Models;

namespace Famoria.Application.Services;

public class EmailPersistenceService : IEmailPersistenceService
{
    private readonly BlobContainerClient _blobContainerClient;
    private readonly IRepository<FamilyItem> _repository;
    private readonly ILogger<EmailPersistenceService> _logger;

    public EmailPersistenceService(
        BlobContainerClient blobContainerClient,
        IRepository<FamilyItem> repository,
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

            var toList = mime.To.Mailboxes.Take(20).Select(m => m.Address).ToList();
            var ccList = mime.Cc.Mailboxes.Take(20).Select(m => m.Address).ToList();

            // Upload original .eml
            var emlBlobPath = $"{familyId}/email/{itemId}/original.eml";
            var emlBlobClient = _blobContainerClient.GetBlobClient(emlBlobPath);
            await emlBlobClient.UploadAsync(new BinaryData(rawEmail.Content), overwrite: true, cancellationToken).ConfigureAwait(false);

            // Upload attachments
            var attachments = new List<AttachmentInfo>();
            foreach (var attachment in mime.Attachments)
            {
                if (attachment is MimePart part)
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
                To = toList.Count > 0 ? toList : null,
                Cc = ccList.Count > 0 ? ccList : null,
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
                Status = FamilyItemStatus.New,
                CreatedAt = now,
                ModifiedAt = now
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
