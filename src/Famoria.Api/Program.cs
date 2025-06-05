using Famoria.Application;
using Famoria.Application.Interfaces;
using Famoria.Application.Models;
using Famoria.Infrastructure;
using Famoria.Domain.Entities;
using Famoria.Domain.Enums;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder
    .AddServiceDefaults()
    .AddInfrastructure(builder.Configuration.Get<AppSettings>()!)
    .AddApiServices();

builder.Services.AddControllers();
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

builder
    .AddInfrastructure(builder.Configuration.Get<AppSettings>()!)
    .AddApiServices();

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
       IUserLinkedAccountService linkedAccount,
       IAesCryptoService crypto,
       ILogger<Program> log,
       CancellationToken ct) =>
{
    var parts     = state.Split(':');
    var familyId  = parts[0];
    var userId    = parts[1];
    var token = await google.ExchangeCodeAsync(code, ct);
    var conn = new UserLinkedAccount
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
    await linkedAccount.UpsertAsync(conn, ct);
    return Results.Ok("Gmail connected!");
});

app.Run();
