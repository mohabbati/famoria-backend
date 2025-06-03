using Famoria.Application.Features.FetchEmails;
using Famoria.Application.Interfaces;
using Microsoft.Extensions.Logging;
using Moq;

namespace Famoria.Unit.Tests.Features;

public class FetchEmailsHandlerTests
{
    private readonly Mock<IEmailFetcher> _fetcherMock = new();
    private readonly Mock<IEmailPersistenceService> _persistenceMock = new();
    private readonly Mock<ILogger<FetchEmailsHandler>> _loggerMock = new();
    private readonly FetchEmailsHandler _handler;
    private readonly DateTime _since = DateTime.UtcNow.AddHours(-1);

    public FetchEmailsHandlerTests()
    {
        _handler = new FetchEmailsHandler(_fetcherMock.Object, _persistenceMock.Object, _loggerMock.Object);
    }

    [Fact]
    public async Task Handle_AllEmailsPersisted_ReturnsCount()
    {
        var emails = new List<string> { "eml1", "eml2" };
        _fetcherMock.Setup(x => x.GetNewEmailsAsync("user", "token", _since, It.IsAny<CancellationToken>())).ReturnsAsync(emails);
        _persistenceMock.Setup(x => x.PersistAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync("id");
        var cmd = new FetchEmailsCommand("fam", "user", "token", _since);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.Equal(2, result);
        _persistenceMock.Verify(x => x.PersistAsync(It.IsAny<string>(), "fam", It.IsAny<CancellationToken>()), Times.Exactly(2));
        _loggerMock.Verify(x => x.Log(
            It.IsAny<LogLevel>(),
            It.IsAny<EventId>(),
            It.IsAny<It.IsAnyType>(),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SomeEmailsFailToPersist_LogsErrorAndReturnsSuccessCount()
    {
        var emails = new List<string> { "eml1", "eml2", "eml3" };
        _fetcherMock.Setup(x => x.GetNewEmailsAsync(It.IsAny<string>(), It.IsAny<string>(), _since, It.IsAny<CancellationToken>())).ReturnsAsync(emails);
        _persistenceMock.SetupSequence(x => x.PersistAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("id1")
            .ThrowsAsync(new Exception("fail"))
            .ReturnsAsync("id3");
        var cmd = new FetchEmailsCommand("fam", "user", "token", _since);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.Equal(2, result);
        _loggerMock.Verify(x => x.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Failed to persist email for family")),
            It.IsAny<Exception>(),
            (Func<It.IsAnyType, Exception?, string>)It.IsAny<object>()), Times.Once);
    }

    [Fact]
    public async Task Handle_NoEmails_ReturnsZero()
    {
        _fetcherMock.Setup(x => x.GetNewEmailsAsync(It.IsAny<string>(), It.IsAny<string>(), _since, It.IsAny<CancellationToken>())).ReturnsAsync(new List<string>());
        var cmd = new FetchEmailsCommand("fam", "user", "token", _since);

        var result = await _handler.Handle(cmd, CancellationToken.None);

        Assert.Equal(0, result);
    }

    [Fact]
    public async Task Handle_CancellationToken_Propagates()
    {
        var cts = new CancellationTokenSource();
        cts.Cancel();
        _fetcherMock.Setup(x => x.GetNewEmailsAsync(It.IsAny<string>(), It.IsAny<string>(), _since, cts.Token)).ThrowsAsync(new OperationCanceledException());
        var cmd = new FetchEmailsCommand("fam", "user", "token", _since);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => _handler.Handle(cmd, cts.Token));
    }

    [Fact]
    public async Task Handle_FetcherThrows_Propagates()
    {
        _fetcherMock.Setup(x => x.GetNewEmailsAsync(It.IsAny<string>(), It.IsAny<string>(), _since, It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("fetch fail"));
        var cmd = new FetchEmailsCommand("fam", "user", "token", _since);

        await Assert.ThrowsAsync<Exception>(() => _handler.Handle(cmd, CancellationToken.None));
    }
}
