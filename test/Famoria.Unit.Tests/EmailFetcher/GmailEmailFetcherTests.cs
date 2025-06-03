using Famoria.Application.Interfaces;
using Famoria.Application.Services;

using MailKit;
using MailKit.Search;
using MailKit.Security;

using Microsoft.Extensions.Logging;

using MimeKit;

using Moq;

using Polly;

namespace Famoria.Unit.Tests.EmailFetcher;

public class GmailEmailFetcherTests
{
    private readonly Mock<ILogger<GmailEmailFetcher>> _loggerMock = new();
    private readonly Mock<IImapClientWrapper> _imapClientMock = new();

    private GmailEmailFetcher CreateFetcher(IAsyncPolicy? retryPolicy = null)
    {
        retryPolicy ??= Policy.NoOpAsync();
        return new GmailEmailFetcher(_loggerMock.Object, retryPolicy, _imapClientMock.Object);
    }

    [Fact]
    public async Task GetNewEmailsAsync_ReturnsEmails_WhenFound()
    {
        var retryPolicy = Policy.NoOpAsync();
        var inboxMock = new Mock<IMailFolder>();
        var uids = new List<UniqueId> { new UniqueId(1) };
        var message = new MimeMessage();
        message.Subject = "Test";
        _imapClientMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<SaslMechanism>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.GetInboxAsync(It.IsAny<CancellationToken>())).ReturnsAsync(inboxMock.Object);
        _imapClientMock.Setup(x => x.SearchAsync(inboxMock.Object, It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(uids);
        _imapClientMock.Setup(x => x.GetMessageAsync(inboxMock.Object, uids[0], It.IsAny<CancellationToken>())).ReturnsAsync(message);
        _imapClientMock.Setup(x => x.DisconnectAsync(true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var fetcher = CreateFetcher(retryPolicy);

        var result = await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow, CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetNewEmailsAsync_LogsMessageLevelError()
    {
        var retryPolicy = Policy.NoOpAsync();
        var inboxMock = new Mock<IMailFolder>();
        var uids = new List<UniqueId> { new UniqueId(1) };
        _imapClientMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<SaslMechanism>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.GetInboxAsync(It.IsAny<CancellationToken>())).ReturnsAsync(inboxMock.Object);
        _imapClientMock.Setup(x => x.SearchAsync(inboxMock.Object, It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(uids);
        _imapClientMock.Setup(x => x.GetMessageAsync(inboxMock.Object, uids[0], It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Message error"));
        _imapClientMock.Setup(x => x.DisconnectAsync(true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var fetcher = CreateFetcher(retryPolicy);

        var result = await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow, CancellationToken.None);

        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to fetch or process message UID")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetNewEmailsAsync_LogsError_OnException()
    {
        var retryPolicy = Policy.NoOpAsync();
        _imapClientMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Connection error"));
        var fetcher = CreateFetcher(retryPolicy);

        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow, CancellationToken.None);
        });
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error fetching emails")),
                It.IsAny<Exception>(),
                It.Is<Func<It.IsAnyType, Exception?, string>>((v, t) => true)),
            Times.Once);
    }

    [Fact]
    public async Task GetNewEmailsAsync_RespectsCancellationToken()
    {
        var retryPolicy = Policy.NoOpAsync();
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _imapClientMock
            .Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var fetcher = CreateFetcher(retryPolicy);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow, cts.Token);
        });
    }

    [Fact]
    public async Task GetNewEmailsAsync_RetriesOnTransientError()
    {
        var callCount = 0;
        var retryPolicy = Policy
            .Handle<Exception>()
            .RetryAsync(2, onRetry: (ex, count, ctx) => callCount++);
        _imapClientMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Connection error"));
        var fetcher = CreateFetcher(retryPolicy);

        await Assert.ThrowsAsync<Exception>(async () =>
        {
            await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow, CancellationToken.None);
        });

        Assert.True(callCount >= 1); // Should retry at least once
    }

    [Fact]
    public async Task GetNewEmailsAsync_ReturnsEmptyList_WhenNoMessages()
    {
        var retryPolicy = Policy.NoOpAsync();
        var inboxMock = new Mock<IMailFolder>();
        var uids = new List<UniqueId>();
        _imapClientMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<SaslMechanism>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.GetInboxAsync(It.IsAny<CancellationToken>())).ReturnsAsync(inboxMock.Object);
        _imapClientMock.Setup(x => x.SearchAsync(inboxMock.Object, It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(uids);
        _imapClientMock.Setup(x => x.DisconnectAsync(true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var fetcher = CreateFetcher(retryPolicy);

        var result = await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetNewEmailsAsync_ReturnsAllEmails_WhenMultipleFound()
    {
        var retryPolicy = Policy.NoOpAsync();
        var inboxMock = new Mock<IMailFolder>();
        var uids = new List<UniqueId> { new UniqueId(1), new UniqueId(2) };
        var message1 = new MimeMessage();
        message1.Subject = "Test1";
        var message2 = new MimeMessage();
        message2.Subject = "Test2";
        _imapClientMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<SaslMechanism>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.GetInboxAsync(It.IsAny<CancellationToken>())).ReturnsAsync(inboxMock.Object);
        _imapClientMock.Setup(x => x.SearchAsync(inboxMock.Object, It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(uids);
        _imapClientMock.Setup(x => x.GetMessageAsync(inboxMock.Object, uids[0], It.IsAny<CancellationToken>())).ReturnsAsync(message1);
        _imapClientMock.Setup(x => x.GetMessageAsync(inboxMock.Object, uids[1], It.IsAny<CancellationToken>())).ReturnsAsync(message2);
        _imapClientMock.Setup(x => x.DisconnectAsync(true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var fetcher = CreateFetcher(retryPolicy);

        var result = await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetNewEmailsAsync_ReturnsRawEmlContent()
    {
        var retryPolicy = Policy.NoOpAsync();
        var inboxMock = new Mock<IMailFolder>();
        var uids = new List<UniqueId> { new UniqueId(1) };
        var message = new MimeMessage();
        message.Subject = "TestSubject";
        message.Body = new TextPart("plain") { Text = "Hello world" };
        _imapClientMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<SaslMechanism>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.GetInboxAsync(It.IsAny<CancellationToken>())).ReturnsAsync(inboxMock.Object);
        _imapClientMock.Setup(x => x.SearchAsync(inboxMock.Object, It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(uids);
        _imapClientMock.Setup(x => x.GetMessageAsync(inboxMock.Object, uids[0], It.IsAny<CancellationToken>())).ReturnsAsync(message);
        _imapClientMock.Setup(x => x.DisconnectAsync(true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var fetcher = CreateFetcher(retryPolicy);
        var result = await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow, CancellationToken.None);

        Assert.Single(result);
        Assert.Contains("TestSubject", result[0]);
        Assert.Contains("Hello world", result[0]);
    }

    [Fact]
    public async Task GetNewEmailsAsync_AlwaysDisconnects_OnError()
    {
        var retryPolicy = Policy.NoOpAsync();
        var inboxMock = new Mock<IMailFolder>();
        var uids = new List<UniqueId> { new UniqueId(1) };
        _imapClientMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<SaslMechanism>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.GetInboxAsync(It.IsAny<CancellationToken>())).ReturnsAsync(inboxMock.Object);
        _imapClientMock.Setup(x => x.SearchAsync(inboxMock.Object, It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(uids);
        _imapClientMock.Setup(x => x.GetMessageAsync(inboxMock.Object, uids[0], It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Message error"));
        _imapClientMock.Setup(x => x.DisconnectAsync(true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var fetcher = CreateFetcher(retryPolicy);
        await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow, CancellationToken.None);

        _imapClientMock.Verify(x => x.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }
}
