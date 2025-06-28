using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CosmosKit; // For IRepository
using Famoria.Application.Interfaces; // For IEmailPersistenceService (though testing concrete class)
using Famoria.Application.Services;
using Famoria.Domain.Entities;
using Famoria.Domain.Enums; // For FamilyItemSource
using Microsoft.Extensions.Logging;
using MimeKit;
using Moq;
using Xunit; // For Fact, Assert
using System; // For Exception, BinaryData, DateTimeOffset
using System.IO; // For MemoryStream
using System.Threading; // For CancellationToken
using System.Threading.Tasks; // For Task
using System.Collections.Generic; // For List
using System.Linq; // For Linq extensions like Any()

namespace Famoria.Unit.Tests.EmailFetcher;

public class EmailPersistenceServiceTests
{
    private readonly Mock<BlobContainerClient> _blobContainerMock = new();
    private readonly Mock<IRepository<FamilyItem>> _repositoryMock = new(); // Changed from CosmosClient
    private readonly Mock<ILogger<EmailPersistenceService>> _loggerMock = new();
    private readonly Mock<BlobClient> _blobClientMock = new();

    // Removed Cosmos specific mocks like _dbMock, _containerMock, _settings

    private EmailPersistenceService CreateService() =>
        new(_blobContainerMock.Object, _repositoryMock.Object, _loggerMock.Object);

    private string CreateTestEml(
        string fromName, string fromAddress,
        string toAddress, string ccAddress,
        string subject, string body,
        DateTimeOffset date,
        string attachmentFileName, byte[] attachmentContent, string attachmentMimeType,
        out string actualAttachmentName)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromAddress));
        if (!string.IsNullOrEmpty(toAddress))
            message.To.Add(new MailboxAddress("To Recipient", toAddress));
        if (!string.IsNullOrEmpty(ccAddress))
            message.Cc.Add(new MailboxAddress("CC Recipient", ccAddress));
        message.Subject = subject;
        message.Date = date;

        var textPart = new TextPart("plain") { Text = body };

        if (attachmentFileName != null && attachmentContent != null)
        {
            var attachment = new MimePart(attachmentMimeType)
            {
                Content = new MimeContent(new MemoryStream(attachmentContent)),
                ContentDisposition = new ContentDisposition(ContentDisposition.Attachment),
                FileName = attachmentFileName
            };
            var multipart = new Multipart("mixed") { textPart, attachment };
            message.Body = multipart;
            actualAttachmentName = attachment.FileName;
        }
        else
        {
            message.Body = textPart;
            actualAttachmentName = null!;
        }

        using var ms = new MemoryStream();
        message.WriteTo(ms);
        return Encoding.UTF8.GetString(ms.ToArray());
    }


    [Fact]
    public async Task PersistAsync_PopulatesAllFieldsCorrectly_AndReturnsItemId()
    {
        // Arrange
        var familyId = "fam789";
        var fromName = "Test Sender";
        var fromAddress = "sender@test.com";
        var toAddress = "receiver@test.com";
        var ccAddress = "cc.receiver@test.com";
        var subject = "Detailed Test Email";
        var body = "This is a more detailed email body for testing.";
        var date = new DateTimeOffset(2023, 10, 26, 10, 30, 0, TimeSpan.FromHours(-5));
        var attachmentFileName = "testDoc.pdf";
        var attachmentContent = Encoding.UTF8.GetBytes("This is a test PDF content.");
        var attachmentMimeType = "application/pdf";

        var emlContent = CreateTestEml(fromName, fromAddress, toAddress, ccAddress, subject, body, date, attachmentFileName, attachmentContent, attachmentMimeType, out var actualAttachmentName);

        // Aligning with user feedback: these provider-specific fields from Gmail IMAP are expected to be null.
        string? providerMsgId = null;
        string? providerConvId = null;
        string? providerSyncToken = null;
        var labels = new List<string> { "Inbox", "Important" }; // Labels can still be fetched from X-GM-LABELS

        _blobContainerMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(_blobClientMock.Object);
        _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>()); // For both EML and attachment

        FamilyItem capturedFamilyItem = null!;
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<FamilyItem>(), It.IsAny<CancellationToken>()))
            .Callback<FamilyItem, CancellationToken>((fi, ct) => capturedFamilyItem = fi)
            .Returns(Task.CompletedTask);

        var service = CreateService();

        // Act
        var itemId = await service.PersistAsync(emlContent, familyId, providerMsgId, providerConvId, providerSyncToken, labels, CancellationToken.None);

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(itemId));
        Assert.NotNull(capturedFamilyItem);
        Assert.Equal(itemId, capturedFamilyItem.Id);
        Assert.Equal(familyId, capturedFamilyItem.FamilyId);
        Assert.Equal(FamilyItemSource.Email, capturedFamilyItem.Source);

        var payload = Assert.IsType<EmailPayload>(capturedFamilyItem.Payload);
        Assert.Equal(date, payload.ReceivedAt);
        Assert.Equal(subject, payload.Subject);
        Assert.Equal(fromName, payload.SenderName);
        Assert.Equal(fromAddress, payload.SenderEmail);
        Assert.Contains(toAddress, payload.To!);
        Assert.Contains(ccAddress, payload.Cc!);
        Assert.Equal($"{familyId}/email/{itemId}/original.eml", payload.EmlBlobPath);

        Assert.NotNull(payload.Attachments);
        Assert.Single(payload.Attachments);
        var attachmentInfo = payload.Attachments![0];
        Assert.Equal(actualAttachmentName, attachmentInfo.FileName);
        Assert.Equal(attachmentMimeType, attachmentInfo.MimeType);
        Assert.Equal(attachmentContent.Length, attachmentInfo.SizeBytes);
        Assert.Equal($"{familyId}/email/{itemId}/attachments/{actualAttachmentName}", attachmentInfo.BlobPath);

        Assert.Equal(providerMsgId, payload.ProviderMessageId);
        Assert.Equal(providerConvId, payload.ProviderConversationId);
        Assert.Equal(providerSyncToken, payload.ProviderSyncToken);
        Assert.Equal(labels, payload.Labels);

        _blobContainerMock.Verify(x => x.GetBlobClient(It.Is<string>(s => s == $"{familyId}/email/{itemId}/original.eml")), Times.Once);
        _blobContainerMock.Verify(x => x.GetBlobClient(It.Is<string>(s => s == $"{familyId}/email/{itemId}/attachments/{actualAttachmentName}")), Times.Once);
        _blobClientMock.Verify(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()), Times.Exactly(2)); // EML + 1 attachment
        _repositoryMock.Verify(x => x.AddAsync(capturedFamilyItem, It.IsAny<CancellationToken>()), Times.Once);
    }


    [Fact]
    public async Task PersistAsync_PersistsAttachmentsWithCorrectPaths()
    {
        var emlContent = CreateTestEml("s","s@s.s","t@t.t",null,"Sub","Body",DateTimeOffset.Now,"att.txt", new byte[]{1,2,3},"text/plain", out var attachmentName);
        var familyId = "fam123";
        var capturedPaths = new List<string>();
        _blobContainerMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(_blobClientMock.Object);
         _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<FamilyItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _blobContainerMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Callback<string>(path => capturedPaths.Add(path)).Returns(_blobClientMock.Object);

        var service = CreateService();
        var itemId = await service.PersistAsync(emlContent, familyId, null, null, null, null, CancellationToken.None);

        Assert.Contains(capturedPaths, p => p == $"{familyId}/email/{itemId}/attachments/{attachmentName}");
    }

    [Fact]
    public async Task PersistAsync_HandlesNoAttachments()
    {
        var emlContent = CreateTestEml("s","s@s.s","t@t.t",null,"Sub","Body",DateTimeOffset.Now, null!, null!, null!,out _);
        var familyId = "fam123";
        _blobContainerMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(_blobClientMock.Object);
        _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>()); // This will be for the EML itself
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<FamilyItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        FamilyItem persistedItem = null!;
        _repositoryMock.Setup(r => r.AddAsync(It.IsAny<FamilyItem>(), It.IsAny<CancellationToken>()))
            .Callback<FamilyItem, CancellationToken>((item, token) => persistedItem = item)
            .Returns(Task.CompletedTask);

        var service = CreateService();
        var itemId = await service.PersistAsync(emlContent, familyId, null, null, null, null, CancellationToken.None);

        var payload = Assert.IsType<EmailPayload>(persistedItem.Payload);
        Assert.Null(payload.Attachments); // Or Assert.Empty if initialized to empty list

        // Verify EML upload happened once, and no attachment uploads
        _blobContainerMock.Verify(x => x.GetBlobClient(It.Is<string>(s => s.EndsWith("original.eml"))), Times.Once);
        _blobContainerMock.Verify(x => x.GetBlobClient(It.Is<string>(s => s.Contains("/attachments/"))), Times.Never);
    }

    [Fact]
    public async Task PersistAsync_ThrowsAndLogs_OnBlobError()
    {
        var emlContent = CreateTestEml("s","s@s.s","t@t.t",null,"Sub","Body",DateTimeOffset.Now,"att.txt", new byte[]{1,2,3},"text/plain", out _);
        var familyId = "fam123";
        _blobContainerMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(_blobClientMock.Object);
        _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Blob error"));
        var service = CreateService();

        await Assert.ThrowsAsync<Exception>(() => service.PersistAsync(emlContent, familyId, null, null, null, null, CancellationToken.None));
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to persist email for family")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task PersistAsync_ThrowsAndLogs_OnRepositoryError() // Changed from CosmosError
    {
        var emlContent = CreateTestEml("s","s@s.s","t@t.t",null,"Sub","Body",DateTimeOffset.Now,"att.txt", new byte[]{1,2,3},"text/plain", out _);
        var familyId = "fam123";
        _blobContainerMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(_blobClientMock.Object);
        _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Mock.Of<Response<BlobContentInfo>>());
        _repositoryMock.Setup(x => x.AddAsync(It.IsAny<FamilyItem>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Repository error"));
        var service = CreateService();

        await Assert.ThrowsAsync<Exception>(() => service.PersistAsync(emlContent, familyId, null, null, null, null, CancellationToken.None));
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
        var emlContent = CreateTestEml("s","s@s.s","t@t.t",null,"Sub","Body",DateTimeOffset.Now,"att.txt", new byte[]{1,2,3},"text/plain", out _);
        var familyId = "fam123";
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _blobContainerMock.Setup(x => x.GetBlobClient(It.IsAny<string>())).Returns(_blobClientMock.Object);
        // Simulate cancellation during blob upload
        _blobClientMock.Setup(x => x.UploadAsync(It.IsAny<Stream>(), true, cts.Token)).ThrowsAsync(new OperationCanceledException());
        var service = CreateService();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => service.PersistAsync(emlContent, familyId, null, null, null, null, cts.Token));
    }
}
