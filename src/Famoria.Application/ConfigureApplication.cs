using System.Text;

using Famoria.Application.Models;
using Famoria.Application.Services;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using Polly;

namespace Famoria.Application;

public static class ConfigureApplication
{
    public static IHostApplicationBuilder AddApplication(this IHostApplicationBuilder builder)
    {
        builder.Services.AddTransient<IUserService, UserService>();
        builder.Services.AddTransient<IFamilyService, FamilyService>();
        builder.Services.AddTransient<IConnectorService, ConnectorService>();
        builder.AddCryptoService();

        return builder;
    }

    private static void AddCryptoService(this IHostApplicationBuilder builder)
    {
        // Register a real implementation for IUserIntegrationConnectionService here
        // builder.Services.AddSingleton<IUserIntegrationConnectionService, CosmosDbIntegrationConnectionService>();
        // Retrieve the AES key from environment or configuration. In production this
        // should come from a secure store such as Azure Key Vault.
        var key = Environment.GetEnvironmentVariable("AUTH_ENCRYPTION_KEY")
                         ?? builder.Configuration["Auth:EncryptionKey"];
        if (string.IsNullOrEmpty(key))
            throw new InvalidOperationException("EncryptionKey not configured");
        var aesKey = Encoding.UTF8.GetBytes(key);
        builder.Services.AddSingleton<IAesCryptoService>(new AesCryptoService(aesKey));
    }

    /// <summary>
    /// Registers services required by the email fetcher worker.
    /// </summary>
    public static IHostApplicationBuilder AddEmailFetcherServices(this IHostApplicationBuilder builder)
    {
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
