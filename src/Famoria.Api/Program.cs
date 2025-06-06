using Famoria.Application;
using Famoria.Application.Interfaces;
using Famoria.Application.Models;
using Famoria.Domain.Entities;
using Famoria.Domain.Enums;
using Famoria.Infrastructure;

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Azure.Cosmos;
using Famoria.Domain.Common;
using Famoria.Application.Services;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder
    .AddServiceDefaults()
    .AddInfrastructure(builder.Configuration.Get<AppSettings>()!)
    .AddApiServices();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "Famoria API", Version = "v1" });
});

builder
    .AddInfrastructure(builder.Configuration.Get<AppSettings>()!)
    .AddApiServices();

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        var secret = builder.Configuration["Auth:Jwt:Secret"]!;
        options.TokenValidationParameters = new()
        {
            ValidateIssuer = false,
            ValidateAudience = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(System.Text.Encoding.UTF8.GetBytes(secret))
        };
    });
builder.Services.AddAuthorization();

var app = builder.Build();

app.MapDefaultEndpoints();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Famoria API V1");
        options.RoutePrefix = string.Empty;
    });
}

app.UseHttpsRedirection();

app.UseAuthentication();

app.UseAuthorization();

app.MapControllers();

// Google sign-in
app.MapGet("/auth/google/signin", (GoogleOAuthHelper helper) =>
{
    var state = Guid.NewGuid().ToString("N");
    var url = helper.BuildAuthUrl(state);
    return Results.Redirect(url);
});

app.MapGet("/auth/google/signin/callback",
async (string code,
       GoogleOAuthHelper helper,
       CosmosClient cosmos,
       CosmosDbSettings settings,
       JwtService jwt,
       CancellationToken ct) =>
{
    var payload = await helper.ExchangeCodeAsync(code, ct);
    var db = cosmos.GetDatabase(settings.DatabaseId);
    var users = db.GetContainer("users");
    var families = db.GetContainer("families");
    FamoriaUser? user = null;
    try
    {
        var resp = await users.ReadItemAsync<FamoriaUser>(payload.Subject, new PartitionKey(payload.Subject));
        user = resp.Resource;
    }
    catch (CosmosException ex) when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
    {
    }

    string familyId;
    if (user is null)
    {
        familyId = IdGenerator.NewId();
        var family = new Family
        {
            Id = familyId,
            DisplayName = payload.Name ?? payload.Email!,
            Members =
            [
                new FamilyMember
                {
                    UserId = payload.Subject,
                    Name = payload.Name ?? payload.Email!,
                    Role = FamilyMemberRole.Parent
                }
            ],
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
        await families.CreateItemAsync(family, new PartitionKey(familyId), cancellationToken: ct);

        user = new FamoriaUser
        {
            Id = payload.Subject,
            Email = payload.Email!,
            Provider = "Google",
            ExternalSub = payload.Subject,
            FamilyIds = [familyId],
            CreatedAt = DateTime.UtcNow,
            ModifiedAt = DateTime.UtcNow
        };
        await users.CreateItemAsync(user, new PartitionKey(user.Id), cancellationToken: ct);
    }
    else
    {
        familyId = user.FamilyIds.First();
    }

    var token = jwt.Sign(user.Id, user.Email, familyId);
    var html = $"<script>window.opener.postMessage({{token:'{token}',familyId:'{familyId}'}},'*');window.close();</script>";
    return Results.Content(html, "text/html");
});

// Minimal-API endpoints for Google OAuth
app.MapGet("/auth/google", (IMailOAuthProvider google, HttpContext ctx) =>
{
    var familyId = ctx.Request.Query["familyId"];
    var userId = ctx.User.FindFirst("sub")?.Value ?? "anonymous";
    var email = ctx.User.FindFirst("email")!.Value;
    var state = $"{familyId}:{userId}:{Guid.NewGuid()}";
    var url = google.BuildConsentUrl(state, email);
    return Results.Redirect(url);
});

app.MapGet("/auth/google/callback",
async (string code, string state,
       IMailOAuthProvider google,
       [FromServices] IUserLinkedAccountService linkedAccount,
       IAesCryptoService crypto,
       ILogger<Program> log,
       CancellationToken ct) =>
{
    var parts = state.Split(':');
    var familyId = parts[0];
    var userId = parts[1];
    var token = await google.ExchangeCodeAsync(code, ct);
    var conn = new UserLinkedAccount
    {
        FamilyId = familyId,
        UserId = userId,
        Provider = "Google",
        Source = FamilyItemSource.Email,
        UserEmail = token.UserEmail,
        AccessToken = crypto.Encrypt(token.AccessToken),
        RefreshToken = token.RefreshToken is null ? null : crypto.Encrypt(token.RefreshToken),
        TokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds),
        IsActive = true
    };
    await linkedAccount.UpsertAsync(conn, ct);
    return Results.Ok("Gmail connected!");
});

app.Run();
