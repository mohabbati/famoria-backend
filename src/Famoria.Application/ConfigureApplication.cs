using System.Reflection;
using Famoria.Application.Interfaces;
using Famoria.Application.Services;
using Famoria.Application.Services.Integrations;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;
using Polly.Retry;

namespace Famoria.Application;

public static class ConfigureApplication
{
    public static IHostApplicationBuilder AddApiServices(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<GoogleAuthSettings>(builder.Configuration.GetSection("Auth:Google"));

        builder.Services.AddHttpClient<IMailOAuthProvider, GmailOAuthProvider>();

        // Register a real implementation for IUserIntegrationConnectionService here
        // builder.Services.AddSingleton<IUserIntegrationConnectionService, CosmosDbIntegrationConnectionService>();
        // Register AesCryptoService with injected key (replace with your actual key retrieval logic)
        var aesKey = Convert.FromBase64String(builder.Configuration["Auth:EncryptionKey"] ?? throw new InvalidOperationException("EncryptionKey not configured"));
        builder.Services.AddSingleton<IAesCryptoService>(new AesCryptoService(aesKey));

        builder.Services.AddSingleton<IUserLinkedAccountService, UserLinkedAccountService>();

        return builder;
    }

    /// <summary>
    /// Registers services required by the email fetcher worker.
    /// </summary>
    public static IHostApplicationBuilder AddEmailFetcherServices(this IHostApplicationBuilder builder)
    {
        var a = Assembly.GetExecutingAssembly();
        builder.Services.AddMediatR(config => config.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly()));
        builder.Services.AddTransient<IEmailFetcher, GmailEmailFetcher>();
        builder.Services.AddTransient<IEmailPersistenceService, EmailPersistenceService>();
        builder.Services.AddTransient<IImapClientWrapper, ImapClientWrapper>();

        builder.Services.AddSingleton<IAsyncPolicy>(
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
