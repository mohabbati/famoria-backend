using Famoria.Application;
using Famoria.Application.Interfaces;
using Famoria.Application.Models;
using Famoria.Application.Services;
using Famoria.Infrastructure;
using Famoria.Application.Integrations.Google;
using Famoria.Domain.Entities;
using Famoria.Domain.Common;
using Famoria.Domain.Enums;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

// Add services to the container.

builder.AddApplication();
builder.AddInfrastructure(builder.Configuration.Get<AppSettings>()!);


builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

// Register Google OAuth settings and provider
builder.Services.Configure<GoogleAuthSettings>(
    builder.Configuration.GetSection("Google"));
builder.Services.AddHttpClient<IMailOAuthProvider, GmailOAuthProvider>();

// Register a real implementation for IUserIntegrationConnectionService here
// builder.Services.AddSingleton<IUserIntegrationConnectionService, CosmosDbIntegrationConnectionService>();
// Register AesCryptoService with injected key (replace with your actual key retrieval logic)
var aesKey = Convert.FromBase64String(builder.Configuration["EncryptionKey"] ?? throw new InvalidOperationException("EncryptionKey not configured"));
builder.Services.AddSingleton<IAesCryptoService>(new AesCryptoService(aesKey));

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

// Minimal-API endpoints for Google OAuth
app.MapGet("/auth/google", (IMailOAuthProvider google, HttpContext ctx) =>
{
    var familyId = ctx.Request.Query["familyId"];
    var userId   = ctx.User.FindFirst("sub")?.Value ?? "anonymous";
    var email    = ctx.User.FindFirst("email")!.Value;
    var state = $"{familyId}:{userId}:{Guid.NewGuid()}";
    var url   = google.BuildConsentUrl(state, email);
    return Results.Redirect(url);
});

app.MapGet("/auth/google/callback",
async (string code, string state,
       IMailOAuthProvider google,
       IUserIntegrationConnectionService store,
       IAesCryptoService crypto,
       ILogger<Program> log,
       CancellationToken ct) =>
{
    var parts     = state.Split(':');
    var familyId  = parts[0];
    var userId    = parts[1];
    var token = await google.ExchangeCodeAsync(code, ct);
    var conn = new UserIntegrationConnection
    {
        FamilyId              = familyId,
        UserId                = userId,
        Provider              = "Google",
        Source                = FamilyItemSource.Email,
        UserEmail             = token.UserEmail,
        AccessToken           = crypto.Encrypt(token.AccessToken),
        RefreshToken          = token.RefreshToken is null ? null : crypto.Encrypt(token.RefreshToken),
        TokenExpiresAtUtc     = DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds),
        IsActive              = true
    };
    await store.UpsertAsync(conn, ct);
    return Results.Ok("Gmail connected!");
});

app.Run();
