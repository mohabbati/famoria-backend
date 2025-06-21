using Famoria.Api.Extensions;
using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Famoria.Api.Controllers;

public class ConnectorController : CustomControllerBase
{
    private readonly IConnectorService _connector;
    private readonly IJwtValidator<GoogleJsonWebSignature.Payload> _validator;
    private readonly IConfiguration _config;

    public ConnectorController(IMediator mediator,
                               IConnectorService connector,
                               IJwtValidator<GoogleJsonWebSignature.Payload> validator,
                               IConfiguration config) : base(mediator)
    {
        _connector = connector;
        _validator = validator;
        _config = config;
    }

    [Authorize]
    [HttpGet("link/gmail")]
    public IActionResult LinkGmail([FromQuery] string returnUrl)
    {
        var email = User.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
        var safe = UrlHelper.GetReturnUrl(_config, returnUrl);
        var props = new AuthenticationProperties
        {
            RedirectUri = $"connector/link/gmail/callback?returnUrl={safe}",
            AllowRefresh = true,
            IsPersistent = true
        };
        return Challenge(props, "GmailLink");
    }

    [Authorize]
    [HttpGet("link/gmail/callback")]
    public async Task<IActionResult> LinkGmailCallback([FromQuery] string returnUrl, CancellationToken cancellationToken)
    {
        var famoriaUserId = User.GetClaim(ClaimTypes.NameIdentifier);

        var origin = UrlHelper.GetOrigin(_config, returnUrl);
        var result = await HttpContext.AuthenticateAsync("GmailLink");
        if (!result.Succeeded || result.Principal is null)
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'Authentication failed'}},'{origin}');window.close();</script>",
                "text/html");
        }
        var accessToken = result.Properties.GetTokenValue("access_token")!;
        var refreshToken = result.Properties.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'No refreshToken token returned'}},'{origin}');window.close();</script>",
                "text/html");
        }

        var linkedEmail = result.Principal.GetClaim(ClaimTypes.Email);
        var expiresAtRaw = result.Properties.GetTokenValue("expires_at")!;
        var expiresAt = DateTimeOffset.Parse(expiresAtRaw).UtcDateTime;
        var familyId = User.FindFirst("family_id")?.Value;
        if (string.IsNullOrEmpty(familyId))
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'No family selected'}},'{origin}');window.close();</script>",
                "text/html");
        }

        await _connector.LinkAsync("Google", familyId, result.Principal, accessToken, refreshToken, expiresAt, cancellationToken);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var html = $"<script>window.opener.postMessage({{gmail:'linked'}},'{origin}');window.close();</script>";
        return Content(html, "text/html");
    }

    [Authorize]
    [HttpGet("link/outlook")]
    public IActionResult LinkOutlook([FromQuery] string returnUrl)
    {
        var safe = UrlHelper.GetReturnUrl(_config, returnUrl);
        var props = new AuthenticationProperties
        {
            RedirectUri = $"connector/link/outlook/callback?returnUrl={safe}",
            AllowRefresh = true,
            IsPersistent = true
        };
        return Challenge(props, "OutlookLink");
    }

    [Authorize]
    [HttpGet("link/outlook/callback")]
    public async Task<IActionResult> LinkOutlookCallback([FromQuery] string returnUrl, CancellationToken cancellationToken)
    {
        var origin = UrlHelper.GetOrigin(_config, returnUrl);
        var result = await HttpContext.AuthenticateAsync("OutlookLink");
        if (!result.Succeeded || result.Principal is null)
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'Authentication failed'}},'{origin}');window.close();</script>",
                "text/html");
        }
        var accessToken = result.Properties.GetTokenValue("access_token")!;
        var refreshToken = result.Properties.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'No refreshToken token returned'}},'{origin}');window.close();</script>",
                "text/html");
        }

        var linkedEmail = result.Principal.GetClaim(ClaimTypes.Email);
        var expiresAtRaw = result.Properties.GetTokenValue("expires_at")!;
        var expiresAt = DateTimeOffset.Parse(expiresAtRaw).UtcDateTime;
        var familyId = User.FindFirst("family_id")?.Value;
        if (string.IsNullOrEmpty(familyId))
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'No family selected'}},'{origin}');window.close();</script>",
                "text/html");
        }

        await _connector.LinkAsync("Microsoft", familyId, result.Principal, accessToken, refreshToken, expiresAt, cancellationToken);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var html = $"<script>window.opener.postMessage({{outlook:'linked'}},'{origin}');window.close();</script>";
        return Content(html, "text/html");
    }
}
