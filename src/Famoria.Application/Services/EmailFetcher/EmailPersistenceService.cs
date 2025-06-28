using Azure.Storage.Blobs;
using CosmosKit;
using Famoria.Application.Interfaces;
using Famoria.Domain.Common;
using Famoria.Domain.Entities;
using Famoria.Domain.Enums;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Text;
using System.Globalization; // Added for DateTimeOffset parsing
using System.Collections.Generic; // Added for List<string>

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

    public async Task<string> PersistAsync(
        string emlContent,
        string familyId,
        string? providerMessageId,
        string? providerConversationId,
        string? providerSyncToken,
        List<string>? labels,
        CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        var itemId = IdFactory.NewGuidId();
        try
        {
            using var emlStream = new MemoryStream(Encoding.UTF8.GetBytes(emlContent));
            var mime = MimeMessage.Load(emlStream);
            emlStream.Position = 0; // Reset stream position for blob upload

            var subject = mime.Subject ?? string.Empty;
            var senderMailbox = mime.From.Mailboxes.FirstOrDefault();
            var senderName = senderMailbox?.Name ?? string.Empty;
            var senderEmail = senderMailbox?.Address ?? string.Empty;

            // Parse Date header to DateTimeOffset, preserving offset
            DateTimeOffset receivedAt = mime.Date; // MimeKit.MimeMessage.Date is already DateTimeOffset

            var toRecipients = mime.To.Mailboxes.Select(mb => mb.Address).Take(20).ToList();
            var ccRecipients = mime.Cc.Mailboxes.Select(mb => mb.Address).Take(20).ToList();

            // Upload original .eml
            var emlBlobPath = $"{familyId}/email/{itemId}/original.eml";
            var emlBlobClient = _blobContainerClient.GetBlobClient(emlBlobPath);
            await emlBlobClient.UploadAsync(emlStream, overwrite: true, cancellationToken).ConfigureAwait(false);

            // Upload attachments
            var attachments = new List<AttachmentInfo>();
            foreach (var attachment in mime.Attachments)
            {
                if (attachment is MimePart part)
                {
                    var fileName = part.FileName ?? IdFactory.NewGuidId();
                    // Sanitize filename if necessary, or use a generic name if part.FileName is null often
                    var attachmentBlobPath = $"{familyId}/email/{itemId}/attachments/{fileName}";
                    var attachmentBlobClient = _blobContainerClient.GetBlobClient(attachmentBlobPath);

                    long sizeBytes = 0;
                    using (var partStream = new MemoryStream())
                    {
                        await part.Content.DecodeToAsync(partStream, cancellationToken).ConfigureAwait(false);
                        sizeBytes = partStream.Length;
                        partStream.Position = 0;
                        await attachmentBlobClient.UploadAsync(partStream, overwrite: true, cancellationToken).ConfigureAwait(false);
                    }

                    attachments.Add(new AttachmentInfo(fileName, part.ContentType.MimeType, sizeBytes, attachmentBlobPath));
                }
            }

            // Create FamilyItem
            var payload = new EmailPayload
            {
                ReceivedAt = receivedAt,
                Subject = subject,
                SenderName = senderName,
                SenderEmail = senderEmail,
                To = toRecipients.Any() ? toRecipients : null,
                Cc = ccRecipients.Any() ? ccRecipients : null,
                EmlBlobPath = emlBlobPath,
                Attachments = attachments.Any() ? attachments : null,
                ProviderMessageId = providerMessageId,
                ProviderConversationId = providerConversationId,
                ProviderSyncToken = providerSyncToken,
                Labels = labels?.Any() == true ? labels : null
            };
            var familyItem = new FamilyItem
            {
                Id = itemId,
                FamilyId = familyId,
                Source = FamilyItemSource.Email,
                Payload = payload,
                Status = FamilyItemStatus.New, // Assuming status remains 'New' initially
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
