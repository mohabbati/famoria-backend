using Famoria.Application.Interfaces;
using Famoria.Application.Services.Auth;

using Google.Apis.Auth;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Famoria.Api.Controllers;

[ApiController]
[Route("[controller]")]
public class ConnectorController : ControllerBase
{
    private readonly GmailLinkService _gmailLink;
    private readonly IJwtValidator<GoogleJsonWebSignature.Payload> _validator;

    public ConnectorController(GmailLinkService gmailLink, IJwtValidator<GoogleJsonWebSignature.Payload> validator)
    {
        _gmailLink = gmailLink;
        _validator = validator;
    }

    [Authorize]
    [HttpGet("link/gmail")]
    public IActionResult LinkGmail([FromQuery] string returnUrl)
    {
        //var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
        var props = new AuthenticationProperties
        {
            RedirectUri = $"connector/link/gmail/callback?returnUrl={returnUrl}",
            AllowRefresh = true,
            IsPersistent = true
        };
        //props.SetParameter("login_hint", email);
        props.SetParameter("login_hint", "mohabbati@gmail.com");
        return Challenge(props, "GmailLink");
    }

    [Authorize]
    [HttpGet("link/gmail/callback")]
    public async Task<IActionResult> LinkGmailCallback([FromQuery] string returnUrl, CancellationToken cancellationToken)
    {
        var result = await HttpContext.AuthenticateAsync("GmailLink");
        if (!result.Succeeded || result.Principal is null)
        {
            return Content(
                "<script>window.opener.postMessage({error:'Authentication failed'},'*');window.close();</script>",
                "text/html");
        }
        var access = result.Properties.GetTokenValue("access_token")!;
        var refresh = result.Properties.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(refresh))
        {
            return Content(
                "<script>window.opener.postMessage({error:'No refresh token returned'},'*');window.close();</script>",
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
                "<script>window.opener.postMessage({error:'Email mismatch'},'*');window.close();</script>",
                "text/html");
        }
        var expires = int.TryParse(result.Properties.GetTokenValue("expires_in"), out var e) ? e : 0;
        var familyId = User.FindFirst("family_id")?.Value;
        if (string.IsNullOrEmpty(familyId))
        {
            return Content(
                "<script>window.opener.postMessage({error:'No family selected'},'*');window.close();</script>",
                "text/html");
        }

        await _gmailLink.LinkAsync(familyId, result.Principal, access, refresh, expires, cancellationToken);
        await HttpContext.SignOutAsync("GoogleTemp");
        var html = "<script>window.opener.postMessage({gmail:'linked'},'*');window.close();</script>";
        return Content(html, "text/html");
    }
}
