using Famoria.Application.Features.FetchEmails;
using Famoria.Application.Interfaces;
using Famoria.Application.Models; // Required for FetchedEmailData
using Microsoft.Extensions.Logging;
using Moq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Famoria.Unit.Tests.EmailFetcher;

public class FetchEmailsHandlerTests
{
    private readonly Mock<IEmailFetcher> _fetcherMock = new();
    private readonly Mock<IEmailPersistenceService> _persistenceMock = new();
    private readonly Mock<ILogger<FetchEmailsHandler>> _loggerMock = new();
    private readonly FetchEmailsHandler _handler;
    private readonly DateTime _since = DateTime.UtcNow.AddHours(-1); // Keep a fixed 'since' for consistency

    public FetchEmailsHandlerTests()
    {
        _handler = new FetchEmailsHandler(_fetcherMock.Object, _persistenceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_AllEmailsPersisted_ReturnsCountAndCallsPersistenceCorrectly()
    {
        var fetchedData = new List<FetchedEmailData>
        {
            new("eml1", "msgId1", "convId1", "sync1", new List<string>{"labelA"}),
            new("eml2", "msgId2", "convId2", "sync2", new List<string>{"labelB"})
        };
        _fetcherMock.Setup(x => x.GetNewEmailsAsync("user", "token", _since, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fetchedData);
        _persistenceMock.Setup(x => x.PersistAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("id");
        var cmd = new FetchEmailsCommand("fam", "user", "token", _since);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.Equal(2, result);
        _persistenceMock.Verify(x => x.PersistAsync("eml1", "fam", "msgId1", "convId1", "sync1", It.Is<List<string>>(l => l.Contains("labelA")), It.IsAny<CancellationToken>()), Times.Once);
        _persistenceMock.Verify(x => x.PersistAsync("eml2", "fam", "msgId2", "convId2", "sync2", It.Is<List<string>>(l => l.Contains("labelB")), It.IsAny<CancellationToken>()), Times.Once);
        _loggerMock.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never);
    }

    [Fact]
    public async Task Handle_SomeEmailsFailToPersist_LogsErrorAndReturnsSuccessCount()
    {
        var fetchedData = new List<FetchedEmailData>
        {
            new("eml1", "msgId1", null, null, null),
            new("eml2Failing", "msgId2", null, null, null), // This one will fail
            new("eml3", "msgId3", null, null, null)
        };
        _fetcherMock.Setup(x => x.GetNewEmailsAsync(It.IsAny<string>(), It.IsAny<string>(), _since, It.IsAny<CancellationToken>()))
            .ReturnsAsync(fetchedData);

        _persistenceMock.Setup(x => x.PersistAsync("eml1", "fam", "msgId1", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("id1");
        _persistenceMock.Setup(x => x.PersistAsync("eml2Failing", "fam", "msgId2", null, null, null, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("fail persist"));
        _persistenceMock.Setup(x => x.PersistAsync("eml3", "fam", "msgId3", null, null, null, It.IsAny<CancellationToken>()))
            .ReturnsAsync("id3");

        var cmd = new FetchEmailsCommand("fam", "user", "token", _since);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.Equal(2, result);
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to persist email for family fam, ProviderMessageId msgId2")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoEmails_ReturnsZero()
    {
        _fetcherMock.Setup(x => x.GetNewEmailsAsync(It.IsAny<string>(), It.IsAny<string>(), _since, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<FetchedEmailData>()); // Return empty list of FetchedEmailData
        var cmd = new FetchEmailsCommand("fam", "user", "token", _since);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.Equal(0, result);
        _persistenceMock.Verify(x => x.PersistAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_CancellationToken_PropagatesToFetcher() // Test name more specific
    {
        var cts = new CancellationTokenSource();
        _fetcherMock.Setup(x => x.GetNewEmailsAsync(It.IsAny<string>(), It.IsAny<string>(), _since, cts.Token))
            .ThrowsAsync(new OperationCanceledException(cts.Token)); // Simulate fetcher respecting cancellation
        var cmd = new FetchEmailsCommand("fam", "user", "token", _since);

        cts.Cancel(); // Cancel the token
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _handler.Handle(cmd, cts.Token));
        _persistenceMock.Verify(x => x.PersistAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_FetcherThrows_PropagatesAndLogsNothingItself() // Handler shouldn't log if fetcher throws, fetcher should log.
    {
        _fetcherMock.Setup(x => x.GetNewEmailsAsync(It.IsAny<string>(), It.IsAny<string>(), _since, It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("fetch fail"));
        var cmd = new FetchEmailsCommand("fam", "user", "token", _since);

        await Assert.ThrowsAsync<Exception>(() => _handler.Handle(cmd, CancellationToken.None));
        _loggerMock.Verify(
            x => x.Log(LogLevel.Error, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.Never); // Handler's own logger for persistence errors shouldn't be hit
    }
}
