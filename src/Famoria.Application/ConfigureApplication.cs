using System.Reflection;
using Famoria.Application.Interfaces;
using Famoria.Application.Services;
using Famoria.Application.Services.Auth;
using Famoria.Application.Services.Integrations;
using Google.Apis.Auth;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Polly;

namespace Famoria.Application;

public static class ConfigureApplication
{
    public static IHostApplicationBuilder AddApiServices(this IHostApplicationBuilder builder)
    {
        builder.Services.Configure<GoogleAuthSettings>(builder.Configuration.GetSection("Google"));
        builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("Auth:Jwt"));

        // OAuth flows handled via AspNet.Security.OAuth.Google
        
        // Register the Google JWT validator
        builder.Services.AddSingleton<IJwtValidator<GoogleJsonWebSignature.Payload>, GoogleJwtValidator>();

        // Register a real implementation for IUserIntegrationConnectionService here
        // builder.Services.AddSingleton<IUserIntegrationConnectionService, CosmosDbIntegrationConnectionService>();
        // Retrieve the AES key from environment or configuration. In production this
        // should come from a secure store such as Azure Key Vault.
        var keyBase64 = Environment.GetEnvironmentVariable("AUTH_ENCRYPTION_KEY")
                         ?? builder.Configuration["Auth:EncryptionKey"];
        if (string.IsNullOrEmpty(keyBase64))
            throw new InvalidOperationException("EncryptionKey not configured");
        var aesKey = Convert.FromBase64String(keyBase64);
        builder.Services.AddSingleton<IAesCryptoService>(new AesCryptoService(aesKey));

        builder.Services.AddSingleton<IUserLinkedAccountService, UserLinkedAccountService>();
        builder.Services.AddSingleton<JwtService>();
        builder.Services.AddTransient<GoogleSignInService>();
        builder.Services.AddTransient<GmailLinkService>();
        builder.Services.AddTransient<FamilyCreationService>();

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
