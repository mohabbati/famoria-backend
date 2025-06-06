using Azure.Storage.Blobs;
using CosmosKit;
using Famoria.Application.Interfaces;
using Famoria.Domain.Common;
using Famoria.Domain.Entities;
using Famoria.Domain.Enums;
using Microsoft.Extensions.Logging;
using System.Text;

using MimeKit;

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
        _blobContainerClient = blobContainerClient;
        _logger = logger;
        _repository = repository;
    }

    public async Task<string> PersistAsync(string emlContent, string familyId, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var itemId = IdGenerator.NewId();
        try
        {
            var mime = MimeMessage.Load(new MemoryStream(Encoding.UTF8.GetBytes(emlContent)));
            var subject = mime.Subject ?? string.Empty;
            var sender = mime.From.Mailboxes.FirstOrDefault()?.Address ?? string.Empty;
            var receivedAt = mime.Date.UtcDateTime;

            // Upload original .eml
            var emlBlobPath = $"{familyId}/email/{itemId}/original.eml";
            var emlBlobClient = _blobContainerClient.GetBlobClient(emlBlobPath);
            await emlBlobClient.UploadAsync(new BinaryData(emlContent), overwrite: true, cancellationToken).ConfigureAwait(false);

            // Upload attachments
            var attachmentBlobPaths = new List<string>();
            foreach (var attachment in mime.Attachments)
            {
                if (attachment is MimePart part)
                {
                    var fileName = part.FileName ?? IdGenerator.NewId();
                    var attachmentBlobPath = $"{familyId}/email/{itemId}/attachments/{fileName}";
                    var attachmentBlobClient = _blobContainerClient.GetBlobClient(attachmentBlobPath);
                    await using var stream = new MemoryStream();
                    await part.Content.DecodeToAsync(stream, cancellationToken).ConfigureAwait(false);
                    stream.Position = 0;
                    await attachmentBlobClient.UploadAsync(stream, overwrite: true, cancellationToken).ConfigureAwait(false);
                    attachmentBlobPaths.Add(attachmentBlobPath);
                }
            }

            // Create FamilyItem
            var payload = new EmailPayload
            {
                ReceivedAt = receivedAt,
                Subject = subject,
                Sender = sender,
                EmlBlobPath = emlBlobPath,
                AttachmentBlobPaths = attachmentBlobPaths
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
