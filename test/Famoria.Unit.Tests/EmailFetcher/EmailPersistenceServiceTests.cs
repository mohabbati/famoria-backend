using System.Text;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Famoria.Application.Services;
using Famoria.Application.Interfaces;
using Famoria.Domain.Entities;
using Microsoft.Extensions.Logging;
using CosmosKit;

using MimeKit;

using Moq;

namespace Famoria.Unit.Tests.EmailFetcher;

public class EmailPersistenceServiceTests
{
    private readonly Mock<BlobContainerClient> _blobContainerMock = new();
    private readonly Mock<IRepository<FamilyItem>> _repositoryMock = new();
    private readonly Mock<ILogger<EmailPersistenceService>> _loggerMock = new();
    private readonly Mock<BlobClient> _blobClientMock = new();

    private EmailPersistenceService CreateService() =>
        new EmailPersistenceService(_blobContainerMock.Object, _repositoryMock.Object, _loggerMock.Object);

    private string CreateEmlWithAttachment(out string attachmentName)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender Name", "sender@example.com"));
        message.Subject = "Test Subject";
        message.Body = new TextPart("plain") { Text = "This is the body." };
        var attachment = new MimePart("application", "octet-stream")
        {
            Content = new MimeContent(new MemoryStream(Encoding.UTF8.GetBytes("Hello world"))),
            ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
            FileName = "attachment.txt"
        };
        var multipart = new Multipart("mixed") { message.Body, attachment };
        message.Body = multipart;
        attachmentName = attachment.FileName;
        using var ms = new MemoryStream();
        message.WriteTo(ms);
        return Encoding.UTF8.GetString(ms.ToArray());
    }

    [Fact(Skip="Outdated")]
    public async Task PersistAsync_PersistsEmlAndMetadata_ReturnsItemId()
    {
        var emlContent = CreateEmlWithAttachment(out var attachmentName);
        var familyId = "fam123";
        _blobContainerMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(_blobClientMock.Object);
        _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<BinaryData>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Azure.Response<BlobContentInfo>>());
        _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Azure.Response<BlobContentInfo>>());
        FamilyItem? capturedItem = null;
        _repositoryMock.Setup(x => x.UpsertAsync(It.IsAny<FamilyItem>(), It.IsAny<CancellationToken>()))
            .Callback<FamilyItem, CancellationToken>((fi, ct) => capturedItem = fi)
            .ReturnsAsync((FamilyItem fi, CancellationToken ct) => fi);

        var service = CreateService();
        var itemId = await service.PersistAsync(emlContent, familyId, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(itemId));
        _blobContainerMock.Verify(x => x.GetBlobClient(It.Is<string>(s => s.Contains($"{familyId}/email/{itemId}/original.eml"))), Times.Once);
        _blobClientMock.Verify(x => x.UploadAsync(It.IsAny<BinaryData>(), true, It.IsAny<CancellationToken>()), Times.Once);
        _repositoryMock.Verify(x => x.UpsertAsync(
            It.Is<FamilyItem>(f => f.FamilyId == familyId),
            It.IsAny<CancellationToken>()), Times.Once);

        Assert.NotNull(capturedItem);
        var payload = Assert.IsType<EmailPayload>(capturedItem!.Payload);
        Assert.Equal(familyId, capturedItem.FamilyId);
        Assert.Equal(itemId, capturedItem.Id);
        Assert.Equal(Domain.Enums.FamilyItemSource.Email, capturedItem.Source);
        Assert.Equal("Test Subject", payload.Subject);
        Assert.Equal("Sender Name", payload.SenderName);
        Assert.Equal("sender@example.com", payload.SenderEmail);
        Assert.Contains("original.eml", payload.EmlBlobPath);
        Assert.NotNull(payload.Attachments);
        Assert.Single(payload.Attachments!);
    }

    [Fact(Skip="Outdated")]
    public async Task PersistAsync_PersistsAttachmentsWithCorrectPaths()
    {
        var emlContent = CreateEmlWithAttachment(out var attachmentName);
        var familyId = "fam123";
        var capturedPaths = new List<string>();
        _blobContainerMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(_blobClientMock.Object);
        _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<BinaryData>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Azure.Response<BlobContentInfo>>());
        _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>())).Callback<object, bool, CancellationToken>((stream, overwrite, ct) => { }).ReturnsAsync(Mock.Of<Azure.Response<BlobContentInfo>>());
        FamilyItem? capturedItem = null;
        _repositoryMock.Setup(x => x.UpsertAsync(It.IsAny<FamilyItem>(), It.IsAny<CancellationToken>()))
            .Callback<FamilyItem, CancellationToken>((fi, ct) => capturedItem = fi)
            .ReturnsAsync((FamilyItem fi, CancellationToken ct) => fi);
        _blobContainerMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Callback<string>(path => capturedPaths.Add(path)).Returns(_blobClientMock.Object);

        var service = CreateService();
        var itemId = await service.PersistAsync(emlContent, familyId, CancellationToken.None);

        Assert.Contains(capturedPaths, p => p.Contains($"{familyId}/email/{itemId}/attachments/{attachmentName}"));
        Assert.NotNull(capturedItem);
        var attachment = Assert.Single(((EmailPayload)capturedItem!.Payload).Attachments!);
        Assert.Equal(attachmentName, attachment.FileName);
    }

    [Fact(Skip="Outdated")]
    public async Task PersistAsync_HandlesNoAttachments()
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender Name", "sender@example.com"));
        message.Subject = "No Attachments";
        message.Body = new TextPart("plain") { Text = "Body only." };
        using var ms = new MemoryStream();
        message.WriteTo(ms);
        var emlContent = Encoding.UTF8.GetString(ms.ToArray());
        var familyId = "fam123";
        _blobContainerMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(_blobClientMock.Object);
        _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<BinaryData>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Azure.Response<BlobContentInfo>>());
        FamilyItem? capturedItem = null;
        _repositoryMock.Setup(x => x.UpsertAsync(It.IsAny<FamilyItem>(), It.IsAny<CancellationToken>()))
            .Callback<FamilyItem, CancellationToken>((fi, ct) => capturedItem = fi)
            .ReturnsAsync((FamilyItem fi, CancellationToken ct) => fi);

        var service = CreateService();
        var itemId = await service.PersistAsync(emlContent, familyId, CancellationToken.None);

        _blobClientMock.Verify(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Never);
        Assert.NotNull(capturedItem);
        Assert.Null(((EmailPayload)capturedItem!.Payload).Attachments);
    }

    [Fact(Skip="Outdated")]
    public async Task PersistAsync_ThrowsAndLogs_OnBlobError()
    {
        var emlContent = CreateEmlWithAttachment(out _);
        var familyId = "fam123";
        _blobContainerMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(_blobClientMock.Object);
        _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<BinaryData>(), true, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Blob error"));
        var service = CreateService();

        await Assert.ThrowsAsync<Exception>(() => service.PersistAsync(emlContent, familyId, CancellationToken.None));
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to persist email for family")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact(Skip="Outdated")]
    public async Task PersistAsync_ThrowsAndLogs_OnRepositoryError()
    {
        var emlContent = CreateEmlWithAttachment(out _);
        var familyId = "fam123";
        _blobContainerMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(_blobClientMock.Object);
        _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<BinaryData>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Azure.Response<BlobContentInfo>>());
        _repositoryMock.Setup(x => x.UpsertAsync(It.IsAny<FamilyItem>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Repo error"));
        var service = CreateService();

        await Assert.ThrowsAsync<Exception>(() => service.PersistAsync(emlContent, familyId, CancellationToken.None));
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to persist email for family")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact(Skip="Outdated")]
    public async Task PersistAsync_Throws_OnCancellation()
    {
        var emlContent = CreateEmlWithAttachment(out _);
        var familyId = "fam123";
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _blobContainerMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(_blobClientMock.Object);
        _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<BinaryData>(), true, It.IsAny<CancellationToken>())).ThrowsAsync(new OperationCanceledException());
        var service = CreateService();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.PersistAsync(emlContent, familyId, cts.Token));
    }
}
