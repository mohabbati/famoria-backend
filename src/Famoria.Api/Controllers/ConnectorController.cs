using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims;

namespace Famoria.Api.Controllers;

public class ConnectorController : CustomControllerBase
{
    private readonly IConnectorService _connector;
    private readonly IConfiguration _config;

    public ConnectorController(IMediator mediator,
                               IConnectorService connector,
                               IConfiguration config) : base(mediator)
    {
        _connector = connector;
        _config = config;
    }

    [Authorize]
    [HttpGet("link/gmail")]
    public IActionResult LinkGmail([FromQuery] string returnUrl)
    {
        var email = User.GetClaim(ClaimTypes.Email);
        var safeReturnUrl = UrlHelper.GetReturnUrl(_config, returnUrl);
        var props = new AuthenticationProperties
        {
            RedirectUri = $"connector/link/gmail/callback?returnUrl={safeReturnUrl}",
            AllowRefresh = true,
            IsPersistent = true
        };
        return Challenge(props, "GmailLink");
    }

    [Authorize]
    [HttpGet("link/gmail/callback")]
    public async Task<IActionResult> LinkGmailCallback([FromQuery] string returnUrl, CancellationToken cancellationToken)
    {
        var famoriaUserId = User.FamoriaUserId();

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

        var linkedEmail = result.Principal.Email();
        var expiresAtRaw = result.Properties.GetTokenValue("expires_at")!;
        var expiresAt = DateTimeOffset.Parse(expiresAtRaw).UtcDateTime;
        var familyId = User.FamilyId();
        if (string.IsNullOrEmpty(familyId))
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'No family selected'}},'{origin}');window.close();</script>",
                "text/html");
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        await _connector.LinkAsync("Google", familyId, result.Principal, accessToken, refreshToken, expiresAt, cancellationToken);

        var html = $"<script>window.opener.postMessage({{gmail:'linked'}},'{origin}');window.close();</script>";

        return Content(html, "text/html");
    }

    [Authorize]
    [HttpGet("link/outlook")]
    public IActionResult LinkOutlook([FromQuery] string returnUrl)
    {
        var safeReturnUrl = UrlHelper.GetReturnUrl(_config, returnUrl);
        var props = new AuthenticationProperties
        {
            RedirectUri = $"connector/link/outlook/callback?returnUrl={safeReturnUrl}",
            AllowRefresh = true,
            IsPersistent = true
        };
        return Challenge(props, "OutlookLink");
    }

    [Authorize]
    [HttpGet("link/outlook/callback")]
    public async Task<IActionResult> LinkOutlookCallback([FromQuery] string returnUrl, CancellationToken cancellationToken)
    {
        var safeReturnUrl = UrlHelper.GetOrigin(_config, returnUrl);
        var result = await HttpContext.AuthenticateAsync("OutlookLink");
        if (!result.Succeeded || result.Principal is null)
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'Authentication failed'}},'{safeReturnUrl}');window.close();</script>",
                "text/html");
        }
        var accessToken = result.Properties.GetTokenValue("access_token")!;
        var refreshToken = result.Properties.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(refreshToken))
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'No refreshToken token returned'}},'{safeReturnUrl}');window.close();</script>",
                "text/html");
        }

        var linkedEmail = result.Principal.Email();
        var expiresAtRaw = result.Properties.GetTokenValue("expires_at")!;
        var expiresAt = DateTimeOffset.Parse(expiresAtRaw).UtcDateTime;
        var familyId = User.FamilyId();
        if (string.IsNullOrEmpty(familyId))
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'No family selected'}},'{safeReturnUrl}');window.close();</script>",
                "text/html");
        }

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        await _connector.LinkAsync("Microsoft", familyId, result.Principal, accessToken, refreshToken, expiresAt, cancellationToken);

        var html = $"<script>window.opener.postMessage({{outlook:'linked'}},'{safeReturnUrl}');window.close();</script>";

        return Content(html, "text/html");
    }
}
