using Azure.Storage.Blobs;
using Famoria.Application.Exceptions;
using Famoria.Application.Models;
using Microsoft.Extensions.Logging;
using MimeKit;
using System.Text;
using UglyToad.PdfPig;

namespace Famoria.Application.Services;

public class EmailService : IEmailService
{
    private readonly BlobContainerClient _blobContainerClient;
    private readonly ICosmosRepository<FamilyItem> _repository;
    private readonly ILogger<EmailService> _logger;

    public EmailService(
        BlobContainerClient blobContainerClient,
        ICosmosRepository<FamilyItem> repository,
        ILogger<EmailService> logger)
    {
        _logger = logger;
        _repository = repository;
        _blobContainerClient = blobContainerClient;
    }

    private const long AttachmentSizeLimit = 20 * 1024 * 1024; // 20 MB

    public async Task<string> DownloadBlobAsync(string blobPath, CancellationToken cancellationToken)
    {
        // 1) Download the .eml blob into memory
        var blobClient = _blobContainerClient.GetBlobClient(blobPath);
        await using var emlMs = new MemoryStream();
        await blobClient.DownloadToAsync(emlMs, cancellationToken);
        emlMs.Position = 0;

        // 2) Parse & extract via IEmailParserService
        string rawContent;
        using (var reader = new StreamReader(emlMs, Encoding.UTF8))
            rawContent = await reader.ReadToEndAsync(cancellationToken);

        return rawContent;
    }

    public async Task<MimeMessage> ParseAsync(string rawEmailContent, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream(Encoding.UTF8.GetBytes(rawEmailContent));
        return await MimeMessage.LoadAsync(ms, cancellationToken);
    }

    public async Task<string> ExtractTextAsync(string rawEmailContent, CancellationToken cancellationToken)
    {
        var message = await ParseAsync(rawEmailContent, cancellationToken);

        // 1) Body text or stripped HTML
        var bodyText = message.TextBody
                   ?? StripHtml(message.HtmlBody ?? string.Empty);

        // 2) PDF attachments
        var sb = new StringBuilder();
        long totalSize = 0;

        var pdfAttachments = message.BodyParts
            .OfType<MimePart>()
            .Where(p => p.IsAttachment && p.ContentType.MimeType == "application/pdf");

        foreach (var part in pdfAttachments)
        {
            await using var mem = new MemoryStream();
            await part.Content.DecodeToAsync(mem, cancellationToken);
            totalSize += mem.Length;
            if (totalSize > AttachmentSizeLimit)
                throw new OversizeBlobException();

            mem.Position = 0;
            using var pdf = PdfDocument.Open(mem);
            foreach (var page in pdf.GetPages())
                sb.AppendLine(page.Text);
        }

        var pdfText = sb.ToString();
        if (pdfText.Length > 4096)
            pdfText = pdfText.Substring(0, 4096) + "\n\n[...truncated]";

        // 3) Combine
        var segments = new[] { bodyText, pdfText }
                       .Where(s => !string.IsNullOrWhiteSpace(s));
        return string.Join("\n\n----- PDF Attachment -----\n\n", segments);
    }

    private string StripHtml(string html)
    {
        var outBuf = new StringBuilder();
        bool inTag = false;
        foreach (var ch in html)
        {
            if (ch == '<') { inTag = true; continue; }
            if (ch == '>') { inTag = false; continue; }
            if (!inTag) outBuf.Append(ch);
        }
        return outBuf.ToString();
    }

    public async Task<string> PersistAsync(RawEmail rawEmail, string familyId, CancellationToken cancellationToken)
    {
        var itemId = IdFactory.NewGuidId();
        try
        {
            // 1) Upload the raw .eml
            var emlPath = $"{familyId}/{FamilyItemSource.Email.ToString().ToLowerInvariant()}/{itemId}/original.eml";
            var client = _blobContainerClient.GetBlobClient(emlPath);
            await client.UploadAsync(BinaryData.FromString(rawEmail.Content), overwrite: true, cancellationToken: cancellationToken);

            // 2) Parse & extract
            var mime = await ParseAsync(rawEmail.Content, cancellationToken);

            // 3) Build payload
            var payload = new EmailPayload
            {
                ReceivedAt = mime.Date,
                Subject = mime.Subject ?? string.Empty,
                SenderName = mime.From.Mailboxes.FirstOrDefault()?.Name ?? string.Empty,
                SenderEmail = mime.From.Mailboxes.FirstOrDefault()?.Address ?? string.Empty,
                EmlBlobPath = emlPath,
                ProviderMessageId = rawEmail.ProviderMessageId,
                ProviderConversationId = rawEmail.ProviderConversationId,
                ProviderSyncToken = rawEmail.ProviderHistoryId,
                Labels = rawEmail.Labels
            };

            var item = new FamilyItem
            {
                Id = itemId,
                FamilyId = familyId,
                Source = FamilyItemSource.Email,
                Payload = payload,
                Status = FamilyItemStatus.New
            };

            // 4) Persist to Cosmos
            await _repository.AddAsync(item, cancellationToken);
            return itemId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "PersistAsync failed for family {FamilyId}", familyId);
            throw;
        }
    }
}
