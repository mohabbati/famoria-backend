using CosmosKit;
using Famoria.Application.Interfaces;
using Famoria.Application.Services;
using Famoria.Domain.Common;
using Famoria.Domain.Entities;
using Famoria.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;

namespace Famoria.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly GoogleOAuthHelper _helper;
    private readonly IRepository<FamoriaUser> _userRepository;
    private readonly IRepository<Family> _familyRepository;
    private readonly JwtService _jwt;
    private readonly IMailOAuthProvider _google;
    private readonly IUserLinkedAccountService _linkedAccount;
    private readonly IAesCryptoService _crypto;

    public AuthController(
        GoogleOAuthHelper helper,
        JwtService jwt,
        IMailOAuthProvider google,
        IUserLinkedAccountService linkedAccount,
        IAesCryptoService crypto,
        IRepository<FamoriaUser> userRepository,
        IRepository<Family> familyRepository)
    {
        _helper = helper;
        _jwt = jwt;
        _google = google;
        _linkedAccount = linkedAccount;
        _crypto = crypto;
        _userRepository = userRepository;
        _familyRepository = familyRepository;
    }

    [HttpGet("google/signin")]
    public IActionResult GoogleSignIn()
    {
        var state = Guid.NewGuid().ToString("N");
        var url = _helper.BuildAuthUrl(state);
        return Redirect(url);
    }

    [HttpGet("google/signin/callback")]
    public async Task<IActionResult> GoogleSignInCallback(string code, CancellationToken ct)
    {
        var payload = await _helper.ExchangeCodeAsync(code, ct);

        FamoriaUser? user = null;
        try
        {
            var resp = await _userRepository.GetByAsync(new FamoriaUser(payload.Subject), ct);
            user = resp;
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
            await _familyRepository.AddAsync(family, ct);

            user = new FamoriaUser(
                payload.Subject,
                payload.Email!,
                "Google",
                payload.Subject,
                [familyId]
            );
            await _userRepository.AddAsync(user, ct);
        }
        else
        {
            familyId = user.FamilyIds.First();
        }

        var token = _jwt.Sign(user.Id, user.Email, familyId);
        var html = $"<script>window.opener.postMessage({{token:'{token}',familyId:'{familyId}'}},'*');window.close();</script>";
        return Content(html, "text/html");
    }

    [HttpGet("google")]
    public IActionResult LinkGmail()
    {
        var familyId = HttpContext.Request.Query["familyId"];
        var userId = HttpContext.User.FindFirst("sub")?.Value ?? "anonymous";
        var email = HttpContext.User.FindFirst("email")!.Value;
        var state = $"{familyId}:{userId}:{Guid.NewGuid()}";
        var url = _google.BuildConsentUrl(state, email);
        return Redirect(url);
    }

    [HttpGet("google/callback")]
    public async Task<IActionResult> GmailCallback(string code, string state, CancellationToken ct)
    {
        var parts = state.Split(':');
        var familyId = parts[0];
        var userId = parts[1];
        var token = await _google.ExchangeCodeAsync(code, ct);
        var conn = new UserLinkedAccount
        {
            FamilyId = familyId,
            UserId = userId,
            Provider = "Google",
            Source = FamilyItemSource.Email,
            UserEmail = token.UserEmail,
            AccessToken = _crypto.Encrypt(token.AccessToken),
            RefreshToken = token.RefreshToken is null ? null : _crypto.Encrypt(token.RefreshToken),
            TokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds),
            IsActive = true
        };
        await _linkedAccount.UpsertAsync(conn, ct);
        return Ok("Gmail connected!");
    }
}

