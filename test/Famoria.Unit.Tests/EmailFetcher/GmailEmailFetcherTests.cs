using System.Net;
using System.Net.Http;
using System.Text;
using Google.Apis.Http;
using Google;
using GoogleHttpClientFactory = Google.Apis.Http.IHttpClientFactory;
using Famoria.Application.Interfaces;
using Famoria.Application.Services;
using Famoria.Domain.Entities;
using CosmosKit;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using RichardSzalay.MockHttp;
using Xunit;

namespace Famoria.Unit.Tests.EmailFetcher;

public class GmailEmailFetcherTests
{
    private readonly Mock<ILogger<GmailEmailFetcher>> _loggerMock = new();
    private readonly Mock<IRepository<UserLinkedAccount>> _repoMock = new();
    private readonly Mock<IAesCryptoService> _cryptoMock = new();

    private GmailEmailFetcher CreateFetcher(MockHttpMessageHandler mockHttp)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Auth:Google:ClientId"] = "id",
                ["Auth:Google:ClientSecret"] = "secret"
            })
            .Build();
        var factory = new HandlerFactory(mockHttp);
        return new GmailEmailFetcher(
            _loggerMock.Object,
            _repoMock.Object,
            _cryptoMock.Object,
            mockHttp.ToHttpClient(),
            config,
            factory);
    }

    [Fact(Skip="Outdated")]
    public async Task GetNewEmailsAsync_ReturnsEmails_WhenApiReturnsMessages()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://gmail.googleapis.com/gmail/v1/users/me/messages*")
            .Respond("application/json", "{\"messages\":[{\"id\":\"1\"}]}" );
        var rawContent = Convert.ToBase64String(Encoding.UTF8.GetBytes("raw eml"))
            .Replace('+','-').Replace('/','_').TrimEnd('=');
        mockHttp.When("https://gmail.googleapis.com/gmail/v1/users/me/messages/1*")
            .Respond("application/json", $"{{\"raw\":\"{rawContent}\"}}");

        var fetcher = CreateFetcher(mockHttp);
        var result = await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow.AddDays(-1), CancellationToken.None);

        Assert.Single(result);
        Assert.Equal("raw eml", result[0]);
    }

    [Fact(Skip="Outdated")]
    public async Task GetNewEmailsAsync_LogsMessageError_WhenMessageFails()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://gmail.googleapis.com/gmail/v1/users/me/messages*")
            .Respond("application/json", "{\"messages\":[{\"id\":\"1\"}]}" );
        mockHttp.When("https://gmail.googleapis.com/gmail/v1/users/me/messages/1*")
            .Respond(HttpStatusCode.InternalServerError);

        var fetcher = CreateFetcher(mockHttp);
        var result = await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow.AddDays(-1), CancellationToken.None);

        Assert.Empty(result);
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to fetch or process message")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Once);
    }

    [Fact(Skip="Outdated")]
    public async Task GetNewEmailsAsync_Throws_WhenListFails()
    {
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://gmail.googleapis.com/gmail/v1/users/me/messages*")
            .Respond(HttpStatusCode.InternalServerError);

        var fetcher = CreateFetcher(mockHttp);
        await Assert.ThrowsAsync<Google.GoogleApiException>(() => fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow.AddDays(-1), CancellationToken.None));
    }

    [Fact(Skip="Outdated")]
    public async Task GetNewEmailsAsync_RespectsCancellationToken()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        var handler = new MockHttpMessageHandler();
        handler.When("*").Respond(_ => throw new OperationCanceledException(cts.Token));
        var fetcher = CreateFetcher(handler);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => fetcher.GetNewEmailsAsync("user", "token", DateTime.UtcNow.AddDays(-1), cts.Token));
    }

    [Fact(Skip="Outdated")]
    public async Task GetNewEmailsAsync_IncludesCategoryFilters()
    {
        string? query = null;
        var handler = new MockHttpMessageHandler();
        handler.When("https://gmail.googleapis.com/gmail/v1/users/me/messages*")
            .Respond(req =>
            {
                query = req.RequestUri!.Query;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"messages\":[]}")
                };
            });

        var fetcher = CreateFetcher(handler);
        await fetcher.GetNewEmailsAsync("user@example.com", "token", DateTime.UtcNow.AddDays(-1), CancellationToken.None);

        Assert.NotNull(query);
        Assert.Contains("-category:promotions", query);
        Assert.Contains("-category:social", query);
    }

    private class HandlerFactory : GoogleHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;
        public HandlerFactory(HttpMessageHandler handler) => _handler = handler;

        public ConfigurableHttpClient CreateHttpClient(CreateHttpClientArgs args)
        {
            var configurable = new ConfigurableMessageHandler(_handler)
            {
                ApplicationName = args.ApplicationName
            };
            return new ConfigurableHttpClient(configurable);
        }
    }
}
