using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Famoria.Application.Features.FetchEmails;
using MediatR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Famoria.Email.Fetcher.Worker.Tests;

public class EmailFetcherWorkerTests
{
    [Fact]
    public async Task EmailFetcherWorker_ExecutesFetchAndLogsSuccess()
    {
        var mediatorMock = new Mock<IMediator>();
        var loggerMock = new Mock<ILogger<EmailFetcherWorker>>();
        mediatorMock.Setup(m => m.Send(It.IsAny<FetchEmailsCommand>(), It.IsAny<CancellationToken>())).ReturnsAsync(3);
        var worker = new EmailFetcherWorker(loggerMock.Object, mediatorMock.Object, TimeSpan.FromMilliseconds(100));
        var cts = new CancellationTokenSource();
        cts.CancelAfter(300); // Let it run at least one loop
        await worker.StartAsync(cts.Token);
        mediatorMock.Verify(m => m.Send(It.IsAny<FetchEmailsCommand>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fetched and persisted")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EmailFetcherWorker_LogsErrorOnException()
    {
        var mediatorMock = new Mock<IMediator>();
        var loggerMock = new Mock<ILogger<EmailFetcherWorker>>();
        mediatorMock.Setup(m => m.Send(It.IsAny<FetchEmailsCommand>(), It.IsAny<CancellationToken>())).ThrowsAsync(new Exception("fail"));
        var worker = new EmailFetcherWorker(loggerMock.Object, mediatorMock.Object, TimeSpan.FromMilliseconds(100));
        var cts = new CancellationTokenSource();
        cts.CancelAfter(300);
        await worker.StartAsync(cts.Token);
        loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error running FetchEmailsHandler")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EmailFetcherWorker_RespectsCancellationToken()
    {
        var mediatorMock = new Mock<IMediator>();
        var loggerMock = new Mock<ILogger<EmailFetcherWorker>>();
        var worker = new EmailFetcherWorker(loggerMock.Object, mediatorMock.Object, TimeSpan.FromMilliseconds(100));
        var cts = new CancellationTokenSource();
        cts.Cancel();
        await worker.StartAsync(cts.Token);
        mediatorMock.Verify(m => m.Send(It.IsAny<FetchEmailsCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
