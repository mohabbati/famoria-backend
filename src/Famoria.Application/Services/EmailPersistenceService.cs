using Azure.Storage.Blobs;
using Famoria.Application.Interfaces;
using Famoria.Domain.Common;
using Famoria.Domain.Entities;
using Famoria.Domain.Enums;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Text;

namespace Famoria.Application.Services;

public class EmailPersistenceService : IEmailPersistenceService
{
    private readonly BlobContainerClient _blobContainerClient;
    private readonly CosmosClient _cosmosClient;
    private readonly ILogger<EmailPersistenceService> _logger;

    public EmailPersistenceService(
        BlobContainerClient blobContainerClient,
        CosmosClient cosmosClient,
        ILogger<EmailPersistenceService> logger)
    {
        _blobContainerClient = blobContainerClient;
        _cosmosClient = cosmosClient;
        _logger = logger;
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
            var db = _cosmosClient.GetDatabase("FamoriaDb");
            var container = db.GetContainer("family-items");
            await container.CreateItemAsync(familyItem, new PartitionKey(familyId), cancellationToken: cancellationToken).ConfigureAwait(false);

            return itemId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist email for family {FamilyId}: {Message}", familyId, ex.Message);
            throw;
        }
    }
} 
