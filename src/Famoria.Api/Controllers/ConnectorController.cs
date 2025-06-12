using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

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
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
        var safe = UrlHelper.GetReturnUrl(_config, returnUrl);
        var props = new AuthenticationProperties
        {
            RedirectUri = $"connector/link/gmail/callback?returnUrl={safe}",
            AllowRefresh = true,
            IsPersistent = true
        };
        props.SetParameter("login_hint", email);
        return Challenge(props, "GmailLink");
    }

    [Authorize]
    [HttpGet("link/gmail/callback")]
    public async Task<IActionResult> LinkGmailCallback([FromQuery] string returnUrl, CancellationToken cancellationToken)
    {
        var origin = UrlHelper.GetOrigin(_config, returnUrl);
        var result = await HttpContext.AuthenticateAsync("GmailLink");
        if (!result.Succeeded || result.Principal is null)
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'Authentication failed'}},'{origin}');window.close();</script>",
                "text/html");
        }
        var access = result.Properties.GetTokenValue("access_token")!;
        var refresh = result.Properties.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(refresh))
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'No refresh token returned'}},'{origin}');window.close();</script>",
                "text/html");
        }

        //var payload = await _validator.ValidateAsync(idToken);
        //if (!payload.EmailVerified)
        //{
        //    return Content(
        //        "<script>window.opener.postMessage({error:'Unverified email'},'*');window.close();</script>",
        //        "text/html");
        //}

        var linkedEmail = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var currentEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (!string.Equals(linkedEmail, currentEmail, StringComparison.OrdinalIgnoreCase))
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'Email mismatch'}},'{origin}');window.close();</script>",
                "text/html");
        }
        var expires = int.TryParse(result.Properties.GetTokenValue("expires_in"), out var e) ? e : 0;
        var familyId = User.FindFirst("family_id")?.Value;
        if (string.IsNullOrEmpty(familyId))
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'No family selected'}},'{origin}');window.close();</script>",
                "text/html");
        }

        await _connector.LinkAsync("Google", familyId, result.Principal, access, refresh, expires, cancellationToken);
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
        var access = result.Properties.GetTokenValue("access_token")!;
        var refresh = result.Properties.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(refresh))
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'No refresh token returned'}},'{origin}');window.close();</script>",
                "text/html");
        }

        var linkedEmail = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var currentEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (!string.Equals(linkedEmail, currentEmail, StringComparison.OrdinalIgnoreCase))
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'Email mismatch'}},'{origin}');window.close();</script>",
                "text/html");
        }
        var expires = int.TryParse(result.Properties.GetTokenValue("expires_in"), out var e) ? e : 0;
        var familyId = User.FindFirst("family_id")?.Value;
        if (string.IsNullOrEmpty(familyId))
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'No family selected'}},'{origin}');window.close();</script>",
                "text/html");
        }

        await _connector.LinkAsync("Microsoft", familyId, result.Principal, access, refresh, expires, cancellationToken);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        var html = $"<script>window.opener.postMessage({{outlook:'linked'}},'{origin}');window.close();</script>";
        return Content(html, "text/html");
    }
}
