using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authentication.MicrosoftAccount;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Famoria.Api.Extensions;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddAuth(this IServiceCollection services, IConfiguration configuration)
    {
        var googleClientId = configuration["Auth:Google:ClientId"] ??
            throw new NullReferenceException("Google client id not found.");
        var googleClientSecret = configuration["Auth:Google:ClientSecret"] ??
            throw new NullReferenceException("Google client secret not found.");

        var msClientId = configuration["Auth:Microsoft:ClientId"] ??
            throw new NullReferenceException("Microsoft client id not found.");
        var msClientSecret = configuration["Auth:Microsoft:ClientSecret"] ??
            throw new NullReferenceException("Microsoft client secret not found.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Auth:Jwt:Issuer"]!,
                ValidAudience = configuration["Auth:Jwt:Audience"]!,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Auth:Jwt:Secret"]!))
            };

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    context.Token = context.Request.Cookies["ACCESS_TOKEN"];
                    return Task.CompletedTask;
                }
            };
        })
        .AddCookie(options =>
        {
            options.Cookie.SameSite = SameSiteMode.None;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.HttpOnly = true;
            options.ExpireTimeSpan = TimeSpan.FromMinutes(5);
        })
        .AddGoogle("GoogleSignIn", options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.CallbackPath = "/signin-google";
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("email");
            options.Scope.Add("profile");
            options.CorrelationCookie.SameSite = SameSiteMode.None;
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
            options.CorrelationCookie.HttpOnly = true;
            options.CorrelationCookie.Expiration = TimeSpan.FromMinutes(5);
        })
        .AddGoogle("GmailLink", options =>
        {
            options.ClientId = googleClientId;
            options.ClientSecret = googleClientSecret;
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.CallbackPath = "/gmail-link";
            options.Scope.Clear();
            options.Scope.Add("openid");
            options.Scope.Add("email");
            options.Scope.Add("https://www.googleapis.com/auth/gmail.readonly");
            options.AccessType = "offline";
            options.SaveTokens = true;
            options.UsePkce = true;
            options.CorrelationCookie.SameSite = SameSiteMode.None;
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
            options.CorrelationCookie.HttpOnly = true;
            options.CorrelationCookie.Expiration = TimeSpan.FromMinutes(5);
        })
        .AddMicrosoftAccount("MicrosoftSignIn", options =>
        {
            options.ClientId = msClientId;
            options.ClientSecret = msClientSecret;
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.CallbackPath = "/signin-microsoft";
            options.Scope.Clear();
            options.Scope.Add("User.Read");
            options.SaveTokens = true;
            options.CorrelationCookie.SameSite = SameSiteMode.None;
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
            options.CorrelationCookie.HttpOnly = true;
            options.CorrelationCookie.Expiration = TimeSpan.FromMinutes(5);
        })
        .AddMicrosoftAccount("OutlookLink", options =>
        {
            options.ClientId = msClientId;
            options.ClientSecret = msClientSecret;
            options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.CallbackPath = "/outlook-link";
            options.Scope.Clear();
            options.Scope.Add("offline_access");
            options.Scope.Add("https://graph.microsoft.com/Mail.Read");
            options.SaveTokens = true;
            options.CorrelationCookie.SameSite = SameSiteMode.None;
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
            options.CorrelationCookie.HttpOnly = true;
            options.CorrelationCookie.Expiration = TimeSpan.FromMinutes(5);
        });

        services.AddDataProtection();
        services.AddAuthorization();

        return services;
    }
}
