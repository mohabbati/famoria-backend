using Famoria.Application.Interfaces;
using Famoria.Application.Models; // For FetchedEmailData
using Famoria.Application.Services;
using Famoria.Domain.Entities; // For UserLinkedAccount
using Famoria.Domain.Enums; // For IntegrationProvider
using CosmosKit; // For IRepository
using MailKit;
using MailKit.Net.Imap; // For ImapClient (underlying type)
using MailKit.Search;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using Moq;
using System;
using System.Collections.Generic;
using System.IO; // For MemoryStream
using System.Linq;
using System.Net.Http;
using System.Text; // For Encoding
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Famoria.Unit.Tests.EmailFetcher;

public class GmailEmailFetcherTests
{
    private readonly Mock<ILogger<GmailEmailFetcher>> _loggerMock = new();
    private readonly Mock<IImapClientWrapper> _imapClientWrapperMock = new();
    private readonly Mock<IRepository<UserLinkedAccount>> _repoMock = new();
    private readonly Mock<IAesCryptoService> _cryptoMock = new();
    private readonly Mock<IMailFolder> _inboxMock = new();

    private GmailEmailFetcher CreateFetcher()
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Google:ClientId"] = "test-client-id",
                ["Auth:Google:ClientSecret"] = "test-client-secret"
            })
            .Build();

        // _mailKitImapClientMock is removed. All setups will be on _imapClientWrapperMock.
        // _inboxMock will be returned by _imapClientWrapperMock.GetInboxAsync().

        return new GmailEmailFetcher(
            _loggerMock.Object,
            _imapClientWrapperMock.Object, // This is the direct dependency now.
            _repoMock.Object,
            _cryptoMock.Object,
            new HttpClient(new HttpMessageHandlerStub()), // HttpClient can be further mocked if needed
            config);
    }

    private MimeMessage CreateSimpleMimeMessage(string subject, string bodyText, Dictionary<string, string>? extraHeaders = null)
    {
        var message = new MimeMessage();
        message.From.Add(new MailboxAddress("Sender", "sender@example.com"));
        message.To.Add(new MailboxAddress("Receiver", "receiver@example.com"));
        message.Subject = subject;
        message.Body = new TextPart("plain") { Text = bodyText };

        if (extraHeaders != null)
        {
            foreach (var header in extraHeaders)
            {
                // For headers that can appear multiple times like X-GM-LABELS
                if (header.Key.Equals("X-GM-LABELS", StringComparison.OrdinalIgnoreCase) && header.Value.Contains(","))
                {
                    var labels = header.Value.Split(',');
                    foreach (var label in labels)
                    {
                        message.Headers.Add(header.Key, label.Trim());
                    }
                }
                else
                {
                    message.Headers.Add(header.Key, header.Value);
                }
            }
        }
        return message;
    }

    // CreateMessageSummaryMock is no longer needed as we get MimeMessage directly from wrapper
    // and parse headers from it.

    [Fact]
    public async Task GetNewEmailsAsync_ReturnsFetchedEmailData_NoProviderHeaders()
    {
        var uids = new List<UniqueId> { new UniqueId(1) };
        // MimeMessage without X-GM-* headers
        var mimeMessage = CreateSimpleMimeMessage("Test Subject", "Hello World!");
        mimeMessage.Headers.Add(HeaderId.MessageId, "<standard-msg-id@example.com>");

        _imapClientWrapperMock.Setup(w => w.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientWrapperMock.Setup(w => w.AuthenticateAsync(It.IsAny<SaslMechanism>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientWrapperMock.Setup(w => w.GetInboxAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_inboxMock.Object);
        _imapClientWrapperMock.Setup(w => w.SearchAsync(_inboxMock.Object, It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(uids);
        _imapClientWrapperMock.Setup(w => w.GetMessageAsync(_inboxMock.Object, uids[0], It.IsAny<CancellationToken>())).ReturnsAsync(mimeMessage);
        _imapClientWrapperMock.Setup(w => w.DisconnectAsync(true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);


        var fetcher = CreateFetcher();
        var result = await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow.AddDays(-1), CancellationToken.None);

        Assert.Single(result);
        var emailData = result[0];
        Assert.Contains("Test Subject", emailData.EmlContent);
        Assert.Contains("Hello World!", emailData.EmlContent);
        // ProviderMessageId should be null if X-GM-MSGID is not present (standard Message-ID is not used for this field)
        Assert.Null(emailData.ProviderMessageId);
        Assert.Null(emailData.ProviderConversationId);
        Assert.Null(emailData.ProviderSyncToken);
        Assert.Null(emailData.Labels); // No X-GM-LABELS headers
    }

    [Fact]
    public async Task GetNewEmailsAsync_ReturnsFetchedEmailData_WithProviderHeaders_WhenPresent()
    {
        var uids = new List<UniqueId> { new UniqueId(1) };
        var headers = new Dictionary<string, string>
        {
            { "X-GM-MSGID", "gmailMsgId123" },
            { "X-GM-THRID", "gmailThreadId456" },
            // Simulate multiple label headers or a single one with multiple values if your CreateSimpleMimeMessage handles it
            { "X-GM-LABELS", "Inbox,Important" }
        };
        var mimeMessageWithHeaders = CreateSimpleMimeMessage("Test Subject With Headers", "Body content", headers);
        mimeMessageWithHeaders.Headers.Add(HeaderId.MessageId, "<standard-id@example.com>");

        _imapClientWrapperMock.Setup(w => w.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientWrapperMock.Setup(w => w.AuthenticateAsync(It.IsAny<SaslMechanism>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientWrapperMock.Setup(w => w.GetInboxAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_inboxMock.Object);
        _imapClientWrapperMock.Setup(w => w.SearchAsync(_inboxMock.Object, It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(uids);
        _imapClientWrapperMock.Setup(w => w.GetMessageAsync(_inboxMock.Object, uids[0], It.IsAny<CancellationToken>())).ReturnsAsync(mimeMessageWithHeaders);
        _imapClientWrapperMock.Setup(w => w.DisconnectAsync(true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var fetcher = CreateFetcher(); // CreateFetcher now correctly uses only the wrapper for setups
        var result = await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow.AddDays(-1), CancellationToken.None);

        Assert.Single(result);
        var emailData = result[0];
        Assert.Equal("gmailMsgId123", emailData.ProviderMessageId);
        Assert.Equal("gmailThreadId456", emailData.ProviderConversationId);
        Assert.Null(emailData.ProviderSyncToken); // Always null for now
        Assert.NotNull(emailData.Labels);
        Assert.Equal(2, emailData.Labels!.Count);
        Assert.Contains("Inbox", emailData.Labels);
        Assert.Contains("Important", emailData.Labels);
    }


    [Fact]
    public async Task GetNewEmailsAsync_HandlesMessageFetchError_AndContinues()
    {
        var uids = new List<UniqueId> { new UniqueId(1), new UniqueId(2) };
        var mimeMessage2 = CreateSimpleMimeMessage("Second Email", "This one is fine.", new Dictionary<string, string> { { "X-GM-MSGID", "msg2" } });

        _imapClientWrapperMock.Setup(w => w.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientWrapperMock.Setup(w => w.AuthenticateAsync(It.IsAny<SaslMechanism>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientWrapperMock.Setup(w => w.GetInboxAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_inboxMock.Object);
        _imapClientWrapperMock.Setup(w => w.SearchAsync(_inboxMock.Object, It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(uids);
        _imapClientWrapperMock.Setup(w => w.GetMessageAsync(_inboxMock.Object, uids[0], It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("Failed to get message 1"));
        _imapClientWrapperMock.Setup(w => w.GetMessageAsync(_inboxMock.Object, uids[1], It.IsAny<CancellationToken>())).ReturnsAsync(mimeMessage2);
        _imapClientWrapperMock.Setup(w => w.DisconnectAsync(true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var fetcher = CreateFetcher();
        var result = await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow.AddDays(-1), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("msg2", result[0].ProviderMessageId);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to fetch or process message UID 1")), // UID is 1 for the failed message
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetNewEmailsAsync_LogsError_OnOverallException()
    {
        _imapClientWrapperMock.Setup(w => w.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Connection error"));

        var fetcher = CreateFetcher();

        await Assert.ThrowsAsync<Exception>(() => // Removed async from lambda as it's not needed for Assert.ThrowsAsync
        {
            await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow, CancellationToken.None);
        });
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error fetching emails for user@example.com")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact]
    public async Task GetNewEmailsAsync_RespectsCancellationTokenDuringConnect()
    {
        var cts = new CancellationTokenSource();
        _imapClientWrapperMock.Setup(w => w.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), cts.Token))
            .ThrowsAsync(new OperationCanceledException(cts.Token));

        cts.Cancel();
        var fetcher = CreateFetcher();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () =>
        {
            await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow, cts.Token);
        });
    }

    [Fact]
    public async Task GetNewEmailsAsync_ReturnsEmptyList_WhenNoMessagesFound()
    {
        var uids = new List<UniqueId>();
        _imapClientWrapperMock.Setup(w => w.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientWrapperMock.Setup(w => w.AuthenticateAsync(It.IsAny<SaslMechanism>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientWrapperMock.Setup(w => w.GetInboxAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_inboxMock.Object);
        _imapClientWrapperMock.Setup(w => w.SearchAsync(_inboxMock.Object, It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(uids);
        _imapClientWrapperMock.Setup(w => w.DisconnectAsync(true, It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var fetcher = CreateFetcher();
        var result = await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow.AddDays(-1), CancellationToken.None);

        Assert.Empty(result);
        _imapClientWrapperMock.Verify(w => w.GetMessageAsync(It.IsAny<IMailFolder>(), It.IsAny<UniqueId>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task GetNewEmailsAsync_RefreshesTokenOnAuthenticationFailure()
    {
        var userEmail = "user@example.com"; // Ensure these are defined
        var initialAccessToken = "initial-token";
        var refreshedAccessToken = "refreshed-token";

        _imapClientWrapperMock.Setup(w => w.ConnectAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SecureSocketOptions>(), It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _imapClientWrapperMock.SetupSequence(w => w.AuthenticateAsync(It.IsAny<SaslMechanismOAuth2>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new AuthenticationException("Initial auth failed"))
            .Returns(Task.CompletedTask);

        var userAccount = new UserLinkedAccount { FamilyId = "fam1", LinkedAccount = userEmail, Provider = IntegrationProvider.Google, RefreshToken = "enc-refresh", AccessToken = "enc-initial" };
        _repoMock.Setup(r => r.GetAsync(It.IsAny<System.Linq.Expressions.Expression<Func<UserLinkedAccount, bool>>>(), null, It.IsAny<CancellationToken>())) // Added null for the second parameter if it's optional (e.g. orderBy)
            .ReturnsAsync(new List<UserLinkedAccount> { userAccount });
        _cryptoMock.Setup(c => c.Decrypt("enc-refresh")).Returns("dec-refresh-token");
        _cryptoMock.Setup(c => c.Encrypt(refreshedAccessToken)).Returns("enc-refreshed-token");

        var httpHandlerStub = new HttpMessageHandlerStub(content: $"{{\"access_token\":\"{refreshedAccessToken}\",\"expires_in\":3600}}");

        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?> { /* ... */ }).Build(); // Assume config is setup as before

        // Important: Re-setup the CreateFetcher mocks for this specific test if they were general before
        // This test specifically modifies how AuthenticateAsync behaves.
        // For simplicity, assuming CreateFetcher setups are fine or are overridden by specific setups here.
        _imapClientWrapperMock.Setup(w => w.GetInboxAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_inboxMock.Object);
        _imapClientWrapperMock.Setup(w => w.SearchAsync(_inboxMock.Object, It.IsAny<SearchQuery>(), It.IsAny<CancellationToken>())).ReturnsAsync(new List<UniqueId>());

        var fetcher = new GmailEmailFetcher( // Recreating to ensure HttpClient has the right stub
            _loggerMock.Object, _imapClientWrapperMock.Object, _repoMock.Object, _cryptoMock.Object, new HttpClient(httpHandlerStub), config);


        await fetcher.GetNewEmailsAsync(userEmail, initialAccessToken, DateTime.UtcNow.AddDays(-1), CancellationToken.None);

        _imapClientWrapperMock.Verify(w => w.AuthenticateAsync(It.Is<SaslMechanismOAuth2>(s => s.AccessToken == initialAccessToken), It.IsAny<CancellationToken>()), Times.Once);
        _imapClientWrapperMock.Verify(w => w.AuthenticateAsync(It.Is<SaslMechanismOAuth2>(s => s.AccessToken == refreshedAccessToken), It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.UpsertAsync(It.Is<UserLinkedAccount>(ua => ua.AccessToken == "enc-refreshed-token"), It.IsAny<CancellationToken>()), Times.Once);
    }


    private class HttpMessageHandlerStub : HttpMessageHandler
    {
        private readonly string _content;
        public HttpMessageHandlerStub(string content = "{\"access_token\":\"refreshed-test-token\",\"expires_in\":3600}")
        {
            _content = content;
        }
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent(_content) });
    }
}
