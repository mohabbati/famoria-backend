using System.Reflection;
using System.Text;
using Famoria.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Famoria.Application;

public static class ConfigureApplication
{
    public static IHostApplicationBuilder AddApiApp(this IHostApplicationBuilder builder)
    {
        builder.Services.AddMediatR(config => config.RegisterServicesFromAssembly(Assembly.GetAssembly(typeof(ConfigureApplication))!));
        builder.Services.AddTransient<IUserService, UserService>();
        builder.Services.AddTransient<IFamilyService, FamilyService>();
        builder.Services.AddTransient<IConnectorService, ConnectorService>();

        builder.AddCryptoService();

        builder.Services.AddHttpClient<GmailEmailFetcher>();
        builder.Services.AddTransient<IEmailFetcher>(sp => sp.GetRequiredService<GmailEmailFetcher>());
        builder.Services.AddTransient<IEmailService, EmailService>();

        return builder;
    }

    public static IHostApplicationBuilder AddEmailFetcherApp(this IHostApplicationBuilder builder)
    {
        builder.Services.AddMediatR(config => config.RegisterServicesFromAssembly(Assembly.GetAssembly(typeof(ConfigureApplication))!));
        builder.Services.AddTransient<IUserService, UserService>();
        builder.Services.AddTransient<IFamilyService, FamilyService>();
        builder.Services.AddTransient<IConnectorService, ConnectorService>();

        builder.AddCryptoService();

        builder.Services.AddHttpClient<GmailEmailFetcher>();
        builder.Services.AddTransient<IEmailFetcher>(sp => sp.GetRequiredService<GmailEmailFetcher>());
        builder.Services.AddTransient<IEmailService, EmailService>();

        return builder;
    }

    public static IHostApplicationBuilder AddSummarizerApp(this IHostApplicationBuilder builder)
    {
        //builder.Services.AddMediatR(config => config.RegisterServicesFromAssembly(Assembly.GetAssembly(typeof(ConfigureApplication))!));
        //builder.Services.AddTransient<IUserService, UserService>();
        builder.Services.AddTransient<IFamilyService, FamilyService>();
        builder.Services.AddTransient<IConnectorService, ConnectorService>();

        builder.AddCryptoService();

        builder.Services.AddHttpClient<GmailEmailFetcher>();
        builder.Services.AddTransient<IEmailService, EmailService>();

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
}
