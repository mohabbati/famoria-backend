using System.Text;

using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;

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

        services
            .AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Auth:Jwt:Issuer"],
                    ValidAudience = configuration["Auth:Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Auth:Jwt:Secret"]!))
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = ctx =>
                    {
                        ctx.Token = ctx.Request.Cookies["ACCESS_TOKEN"];
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
            .AddGoogle("Google", options =>
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
                //options.Scope.Add("https://mail.google.com/");
                options.Scope.Add("https://www.googleapis.com/auth/gmail.readonly");
                //options.Scope.Add("https://www.googleapis.com/auth/gmail.modify");
                options.AccessType = "offline";
                options.SaveTokens = true;
                options.UsePkce = true;
                options.AdditionalAuthorizationParameters["prompt"] = "consent select_account";
                options.CorrelationCookie.SameSite = SameSiteMode.None;
                options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
                options.CorrelationCookie.HttpOnly = true;
                options.CorrelationCookie.Expiration = TimeSpan.FromMinutes(5);
            })
            .AddMicrosoftAccount("Microsoft", options =>
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
            options.AdditionalAuthorizationParameters["prompt"] = "consent select_account";
            options.CorrelationCookie.SameSite = SameSiteMode.None;
            options.CorrelationCookie.SecurePolicy = CookieSecurePolicy.Always;
            options.CorrelationCookie.HttpOnly = true;
            options.CorrelationCookie.Expiration = TimeSpan.FromMinutes(5);
        });

        services.AddDataProtection();
        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddCustomCors(this IServiceCollection services, IConfiguration configuration)
    {
        // Add CORS
        var frontendUrl = configuration["Auth:FrontendUrl"] ?? "https://localhost:19759";
        services.AddCors(options =>
        {
            options.AddDefaultPolicy(policy =>
            {
                policy.WithOrigins(frontendUrl)
                      .AllowAnyMethod()
                      .AllowAnyHeader()
                      .AllowCredentials();
            });
        });

        return services;
    }

    public static IServiceCollection AddCustomSwagger(this IServiceCollection services, IConfiguration configuration)
    {
        // Configure Swagger with authentication
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "Famoria API", Version = "v1" });

            var apiBaseUrl = configuration["Auth:ApiUrl"] ?? "https://localhost:7001";
            // Define OAuth2 Google authentication scheme
            options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.OAuth2,
                Description = "OAuth2 Authentication",
                Flows = new OpenApiOAuthFlows
                {
                    Implicit = new OpenApiOAuthFlow
                    {
                        AuthorizationUrl = new Uri($"{apiBaseUrl}/auth/signin/google?returnUrl={apiBaseUrl}/swagger/oauth2-redirect.html"),
                        Scopes = new Dictionary<string, string>
                {
                    { "openid", "OpenID Connect" },
                    { "email", "Email information" },
                    { "profile", "User profile" }
                }
                    }
                }
            });

            // Add operation filter to document OAuth2 requirements
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                {
                    new OpenApiSecurityScheme
                    {
                        Reference = new OpenApiReference
                        {
                            Type = ReferenceType.SecurityScheme,
                            Id = "oauth2"
                        }
                    },
                    // Specify the scopes that should be selected by default
                    new[] { "openid", "email", "profile" }
                }
            });
        });

        return services;
    }
}
