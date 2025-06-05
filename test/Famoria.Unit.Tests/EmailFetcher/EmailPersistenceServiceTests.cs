using System.Text;

using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

using Famoria.Application.Models;
using Famoria.Application.Services;
using Famoria.Domain.Entities;

using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Logging;

using MimeKit;

using Moq;

namespace Famoria.Unit.Tests.EmailFetcher;

public class EmailPersistenceServiceTests
{
    private readonly Mock<BlobContainerClient> _blobContainerMock = new();
    private readonly Mock<CosmosClient> _cosmosClientMock = new();
    private readonly Mock<ILogger<EmailPersistenceService>> _loggerMock = new();
    private readonly Mock<BlobClient> _blobClientMock = new();
    private readonly Mock<Database> _dbMock = new();
    private readonly Mock<Container> _containerMock = new();
    private readonly CosmosDbSettings _settings = new() { DatabaseId = "FamoriaDb" };

    private EmailPersistenceService CreateService() =>
        new EmailPersistenceService(_blobContainerMock.Object, _cosmosClientMock.Object, _settings, _loggerMock.Object);

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

    [Fact]
    public async Task PersistAsync_PersistsEmlAndMetadata_ReturnsItemId()
    {
        var emlContent = CreateEmlWithAttachment(out var attachmentName);
        var familyId = "fam123";
        _blobContainerMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(_blobClientMock.Object);
        _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<BinaryData>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Azure.Response<BlobContentInfo>>());
        _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Azure.Response<BlobContentInfo>>());
        _cosmosClientMock.Setup(x => x.GetDatabase(It.IsAny<string>())).Returns(_dbMock.Object);
        _dbMock.Setup(x => x.GetContainer(It.IsAny<string>())).Returns(_containerMock.Object);
        _containerMock.Setup(x => x.CreateItemAsync(
            It.IsAny<FamilyItem>(),
            It.IsAny<PartitionKey>(),
            null,
            It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<ItemResponse<FamilyItem>>());

        var service = CreateService();
        var itemId = await service.PersistAsync(emlContent, familyId, CancellationToken.None);

        Assert.False(string.IsNullOrWhiteSpace(itemId));
        _blobContainerMock.Verify(x => x.GetBlobClient(It.Is<string>(s => s.Contains($"{familyId}/email/{itemId}/original.eml"))), Times.Once);
        _blobClientMock.Verify(x => x.UploadAsync(It.IsAny<BinaryData>(), true, It.IsAny<CancellationToken>()), Times.Once);
        _containerMock.Verify(x => x.CreateItemAsync(
            It.Is<FamilyItem>(f => f.Id == itemId && f.FamilyId == familyId && f.Source == Domain.Enums.FamilyItemSource.Email),
            It.Is<PartitionKey>(pk => pk.ToString() == $"[\"{familyId}\"]"),
            null,
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task PersistAsync_PersistsAttachmentsWithCorrectPaths()
    {
        var emlContent = CreateEmlWithAttachment(out var attachmentName);
        var familyId = "fam123";
        var capturedPaths = new List<string>();
        _blobContainerMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(_blobClientMock.Object);
        _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<BinaryData>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Azure.Response<BlobContentInfo>>());
        _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>())).Callback<object, bool, CancellationToken>((stream, overwrite, ct) => { }).ReturnsAsync(Mock.Of<Azure.Response<BlobContentInfo>>());
        _cosmosClientMock.Setup(x => x.GetDatabase(It.IsAny<string>())).Returns(_dbMock.Object);
        _dbMock.Setup(x => x.GetContainer(It.IsAny<string>())).Returns(_containerMock.Object);
        _containerMock.Setup(x => x.CreateItemAsync(
            It.IsAny<FamilyItem>(),
            It.IsAny<PartitionKey>(),
            null,
            It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<ItemResponse<FamilyItem>>());
        _blobContainerMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Callback<string>(path => capturedPaths.Add(path)).Returns(_blobClientMock.Object);

        var service = CreateService();
        var itemId = await service.PersistAsync(emlContent, familyId, CancellationToken.None);

        Assert.Contains(capturedPaths, p => p.Contains($"{familyId}/email/{itemId}/attachments/{attachmentName}"));
    }

    [Fact]
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
        _cosmosClientMock.Setup(x => x.GetDatabase(It.IsAny<string>())).Returns(_dbMock.Object);
        _dbMock.Setup(x => x.GetContainer(It.IsAny<string>())).Returns(_containerMock.Object);
        _containerMock.Setup(x => x.CreateItemAsync(
            It.IsAny<FamilyItem>(),
            It.IsAny<PartitionKey>(),
            null,
            It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<ItemResponse<FamilyItem>>());

        var service = CreateService();
        var itemId = await service.PersistAsync(emlContent, familyId, CancellationToken.None);

        _blobClientMock.Verify(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
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

    [Fact]
    public async Task PersistAsync_ThrowsAndLogs_OnCosmosError()
    {
        var emlContent = CreateEmlWithAttachment(out _);
        var familyId = "fam123";
        _blobContainerMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(_blobClientMock.Object);
        _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<BinaryData>(), true, It.IsAny<CancellationToken>())).ReturnsAsync(Mock.Of<Azure.Response<BlobContentInfo>>());
        _cosmosClientMock.Setup(x => x.GetDatabase(It.IsAny<string>())).Returns(_dbMock.Object);
        _dbMock.Setup(x => x.GetContainer(It.IsAny<string>())).Returns(_containerMock.Object);
        _containerMock.Setup(x => x.CreateItemAsync(
            It.IsAny<FamilyItem>(),
            It.IsAny<PartitionKey>(),
            null,
            It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Cosmos error"));
        var service = CreateService();

        await Assert.ThrowsAsync<Exception>(() => service.PersistAsync(emlContent, familyId, CancellationToken.None));
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to persist email for family")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
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
