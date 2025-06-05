using Famoria.Application.Interfaces;
using Famoria.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Retry;
using System.Reflection;

namespace Famoria.Application;

public static class ConfigureApplication
{
    public static IHostApplicationBuilder AddApplication(this IHostApplicationBuilder builder)
    {
        builder.Services.AddMediatR(config => config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        
        return builder;
    }

    /// <summary>
    /// Registers services required by the email fetcher worker.
    /// </summary>
    public static IHostApplicationBuilder AddEmailFetcherServices(this IHostApplicationBuilder builder)
    {
        builder.Services.AddScoped<IEmailFetcher, GmailEmailFetcher>();
        builder.Services.AddScoped<IEmailPersistenceService, EmailPersistenceService>();
        builder.Services.AddTransient<IImapClientWrapper, ImapClientWrapper>();

        builder.Services.AddSingleton<AsyncRetryPolicy>(
                    Policy
                        .Handle<MailKit.Net.Imap.ImapProtocolException>()
                        .Or<MailKit.CommandException>()
                        .Or<IOException>()
                        .Or<System.Net.Sockets.SocketException>()
                        .Or<MailKit.Security.AuthenticationException>()
                        .WaitAndRetryAsync(
                            retryCount: 3,
                            sleepDurationProvider: attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt)),
                            onRetry: (exception, timespan, retryCount, context) =>
                            {
                                // Logging is handled in the consumer
                            }
                        )
                );

        return builder;
    }
}
