using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

using Famoria.Application.Features.FetchEmails;
using Famoria.Application.Interfaces;
using Famoria.Application.Models.Dtos;
using Famoria.Domain.Enums;
using Famoria.Email.Fetcher.Worker;

using MediatR;

using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

namespace Famoria.Unit.Tests.EmailFetcher;

public class EmailFetcherWorkerTests
{
    [Fact]
    public async Task EmailFetcherWorker_ExecutesFetchAndLogsSuccess()
    {
        var mediatorMock = new Mock<IMediator>();
        var connectorMock = new Mock<IConnectorService>();
        var cryptoMock = new Mock<IAesCryptoService>();
        var loggerMock = new Mock<ILogger<EmailFetcherWorker>>();

        var accounts = new List<UserLinkedAccountDto>
        {
            new("fam","user@example.com","enc","ref",DateTime.UtcNow.AddHours(-1))
        };
        connectorMock.Setup(x => x.GetByAsync(It.IsAny<IntegrationProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(accounts);
        cryptoMock.Setup(x => x.Decrypt("enc")).Returns("token");
        mediatorMock.Setup(m => m.Send(It.IsAny<FetchEmailsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);
        connectorMock.Setup(x => x.UpdateLastFetchedAsync(It.IsAny<IntegrationProvider>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var worker = new EmailFetcherWorker(loggerMock.Object, mediatorMock.Object, connectorMock.Object, cryptoMock.Object, TimeSpan.FromMilliseconds(50));

        var cts = new CancellationTokenSource();
        cts.CancelAfter(150);
        await worker.StartAsync(cts.Token);
        await Task.Delay(100);

        mediatorMock.Verify(m => m.Send(It.IsAny<FetchEmailsCommand>(), It.IsAny<CancellationToken>()), Times.AtLeastOnce);
        loggerMock.Verify(l => l.Log(
            LogLevel.Information,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Fetched")),
            null,
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EmailFetcherWorker_LogsErrorOnException()
    {
        var mediatorMock = new Mock<IMediator>();
        var connectorMock = new Mock<IConnectorService>();
        var cryptoMock = new Mock<IAesCryptoService>();
        var loggerMock = new Mock<ILogger<EmailFetcherWorker>>();

        connectorMock.Setup(x => x.GetByAsync(It.IsAny<IntegrationProvider>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<UserLinkedAccountDto>
            {
                new("fam","user@example.com","enc","ref",DateTime.UtcNow)
            });
        cryptoMock.Setup(x => x.Decrypt(It.IsAny<string>())).Returns("token");
        mediatorMock.Setup(m => m.Send(It.IsAny<FetchEmailsCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("fail"));

        var worker = new EmailFetcherWorker(loggerMock.Object, mediatorMock.Object, connectorMock.Object, cryptoMock.Object, TimeSpan.FromMilliseconds(50));

        var cts = new CancellationTokenSource();
        cts.CancelAfter(150);
        await worker.StartAsync(cts.Token);
        await Task.Delay(100);

        loggerMock.Verify(l => l.Log(
            LogLevel.Error,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Error processing linked account")),
            It.IsAny<Exception>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task EmailFetcherWorker_RespectsCancellationToken()
    {
        var mediatorMock = new Mock<IMediator>();
        var connectorMock = new Mock<IConnectorService>();
        var cryptoMock = new Mock<IAesCryptoService>();
        var loggerMock = new Mock<ILogger<EmailFetcherWorker>>();

        var worker = new EmailFetcherWorker(loggerMock.Object, mediatorMock.Object, connectorMock.Object, cryptoMock.Object, TimeSpan.FromMilliseconds(50));

        var cts = new CancellationTokenSource();
        cts.Cancel();
        await worker.StartAsync(cts.Token);
        await Task.Delay(50);

        mediatorMock.Verify(m => m.Send(It.IsAny<FetchEmailsCommand>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
