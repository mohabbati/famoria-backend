using Famoria.Application.Interfaces;
using Famoria.Application.Services;

using MailKit;
using MailKit.Search;
using MailKit.Security;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using System.Net.Http;
using System.Collections.Generic;

using MimeKit;

using Moq;


namespace Famoria.Unit.Tests.EmailFetcher;

public class GmailEmailFetcherTests
{
    private readonly Mock<ILogger<GmailEmailFetcher>> _loggerMock = new();
    private readonly Mock<IImapClientWrapper> _imapClientMock = new();
    private readonly Mock<IRepository<UserLinkedAccount>> _repoMock = new();
    private readonly Mock<IAesCryptoService> _cryptoMock = new();

    private GmailEmailFetcher CreateFetcher()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string>
            {
                ["Auth:Google:ClientId"] = "id",
                ["Auth:Google:ClientSecret"] = "secret"
            })
            .Build();
        return new GmailEmailFetcher(
            _loggerMock.Object,
            _imapClientMock.Object,
            _repoMock.Object,
            _cryptoMock.Object,
            new HttpClient(new HttpMessageHandlerStub()),
            config);
    }

    [Fact]
    public async Task GetNewEmailsAsync_ReturnsEmails_WhenFound()
    {
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

        var fetcher = CreateFetcher();

        var result = await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow, CancellationToken.None);

        Assert.Single(result);
    }

    [Fact]
    public async Task GetNewEmailsAsync_LogsMessageLevelError()
    {
        var inboxMock = new Mock<IMailFolder>();
        var uids = new List<UniqueId> { new UniqueId(1) };
        _imapClientMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<SaslMechanism>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.GetInboxAsync(It.IsAny<CancellationToken>())).ReturnsAsync(inboxMock.Object);
        _imapClientMock.Setup(x => x.SearchAsync(inboxMock.Object, It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(uids);
        _imapClientMock.Setup(x => x.GetMessageAsync(inboxMock.Object, uids[0], It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Message error"));
        _imapClientMock.Setup(x => x.DisconnectAsync(true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var fetcher = CreateFetcher();

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
        _imapClientMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Connection error"));
        var fetcher = CreateFetcher();

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
        var cts = new CancellationTokenSource();
        cts.Cancel();

        _imapClientMock
            .Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        var fetcher = CreateFetcher();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow, cts.Token);
        });
    }


    [Fact]
    public async Task GetNewEmailsAsync_ReturnsEmptyList_WhenNoMessages()
    {
        var inboxMock = new Mock<IMailFolder>();
        var uids = new List<UniqueId>();
        _imapClientMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<SaslMechanism>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.GetInboxAsync(It.IsAny<CancellationToken>())).ReturnsAsync(inboxMock.Object);
        _imapClientMock.Setup(x => x.SearchAsync(inboxMock.Object, It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(uids);
        _imapClientMock.Setup(x => x.DisconnectAsync(true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var fetcher = CreateFetcher();

        var result = await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow, CancellationToken.None);

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetNewEmailsAsync_ReturnsAllEmails_WhenMultipleFound()
    {
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

        var fetcher = CreateFetcher();

        var result = await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow, CancellationToken.None);

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public async Task GetNewEmailsAsync_ReturnsRawEmlContent()
    {
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

        var fetcher = CreateFetcher();
        var result = await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow, CancellationToken.None);

        Assert.Single(result);
        Assert.Contains("TestSubject", result[0]);
        Assert.Contains("Hello world", result[0]);
    }

    [Fact]
    public async Task GetNewEmailsAsync_AlwaysDisconnects_OnError()
    {
        var inboxMock = new Mock<IMailFolder>();
        var uids = new List<UniqueId> { new UniqueId(1) };
        _imapClientMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<SaslMechanism>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.GetInboxAsync(It.IsAny<CancellationToken>())).ReturnsAsync(inboxMock.Object);
        _imapClientMock.Setup(x => x.SearchAsync(inboxMock.Object, It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(uids);
        _imapClientMock.Setup(x => x.GetMessageAsync(inboxMock.Object, uids[0], It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Message error"));
        _imapClientMock.Setup(x => x.DisconnectAsync(true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var fetcher = CreateFetcher();
        await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow, CancellationToken.None);

        _imapClientMock.Verify(x => x.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetNewEmailsAsync_Disconnects_WhenSearchFails()
    {
        var inboxMock = new Mock<IMailFolder>();
        _imapClientMock.Setup(x => x.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.AuthenticateAsync(It.IsAny<SaslMechanism>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientMock.Setup(x => x.GetInboxAsync(It.IsAny<CancellationToken>())).ReturnsAsync(inboxMock.Object);
        _imapClientMock.Setup(x => x.SearchAsync(inboxMock.Object, It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Search error"));
        _imapClientMock.Setup(x => x.DisconnectAsync(true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var fetcher = CreateFetcher();

        await Assert.ThrowsAsync<Exception>(() => fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow, CancellationToken.None));

        _imapClientMock.Verify(x => x.DisconnectAsync(true, It.IsAny<CancellationToken>()), Times.Once);
    }

    private class HttpMessageHandlerStub : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK)
            {
                Content = new StringContent("{\"access_token\":\"token\",\"expires_in\":3600}")
            });
    }
}
