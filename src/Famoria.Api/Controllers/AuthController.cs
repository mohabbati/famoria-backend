using Famoria.Application.Services.Auth;
using Famoria.Application.Interfaces;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Famoria.Api.Controllers;

[ApiController]
[Route("auth")]
public class AuthController : ControllerBase
{
    private readonly GoogleSignInService _signIn;
    private readonly GmailLinkService _gmailLink;
    private readonly IGoogleJwtValidator _validator;

    public AuthController(GoogleSignInService signIn, GmailLinkService gmailLink, IGoogleJwtValidator validator)
    {
        _signIn = signIn;
        _gmailLink = gmailLink;
        _validator = validator;
    }

    [HttpGet("signin/google")]
    public IActionResult SignIn()
    {
        var props = new AuthenticationProperties { RedirectUri = "/signin-google" };
        return Challenge(props, "GoogleSignIn");
    }

    [HttpGet("/signin-google")]
    public async Task<IActionResult> SignInCallback(CancellationToken ct)
    {
        var result = await HttpContext.AuthenticateAsync("GoogleTemp");
        if (!result.Succeeded || result.Principal is null)
        {
            var html = "<script>window.opener.postMessage({error:'Authentication failed'},'*');window.close();</script>";
            return Content(html, "text/html");
        }

        var idToken = result.Properties.GetTokenValue("id_token");
        if (string.IsNullOrEmpty(idToken))
        {
            var html = "<script>window.opener.postMessage({error:'Missing ID token'},'*');window.close();</script>";
            return Content(html, "text/html");
        }
        var payload = await _validator.ValidateAsync(idToken);
        if (!payload.EmailVerified)
        {
            var html = "<script>window.opener.postMessage({error:'Unverified email'},'*');window.close();</script>";
            return Content(html, "text/html");
        }

        var token = await _signIn.SignInAsync(result.Principal, ct);
        await HttpContext.SignOutAsync("GoogleTemp");
        var html = $"<script>window.opener.postMessage({{token:'{token}'}},'*');window.close();</script>";
        return Content(html, "text/html");
    }

    [Authorize]
    [HttpGet("link/gmail")]
    public IActionResult LinkGmail()
    {
        var email = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value ?? string.Empty;
        var props = new AuthenticationProperties { RedirectUri = "/link-gmail" };
        props.SetParameter("login_hint", email);
        return Challenge(props, "GmailLink");
    }

    [Authorize]
    [HttpGet("/link-gmail")]
    public async Task<IActionResult> LinkGmailCallback(CancellationToken ct)
    {
        var result = await HttpContext.AuthenticateAsync("GoogleTemp");
        if (!result.Succeeded || result.Principal is null)
        {
            var html = "<script>window.opener.postMessage({error:'Authentication failed'},'*');window.close();</script>";
            return Content(html, "text/html");
        }
        var access = result.Properties.GetTokenValue("access_token")!;
        var refresh = result.Properties.GetTokenValue("refresh_token");
        if (string.IsNullOrEmpty(refresh))
        {
            var html = "<script>window.opener.postMessage({error:'No refresh token returned'},'*');window.close();</script>";
            return Content(html, "text/html");
        }
        var linkedEmail = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var currentEmail = User.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (!string.Equals(linkedEmail, currentEmail, StringComparison.OrdinalIgnoreCase))
        {
            var html = "<script>window.opener.postMessage({error:'Email mismatch'},'*');window.close();</script>";
            return Content(html, "text/html");
        }
        var expires = int.TryParse(result.Properties.GetTokenValue("expires_in"), out var e) ? e : 0;
        var familyId = User.FindFirst("family_id")?.Value;
        if (string.IsNullOrEmpty(familyId))
        {
            var html = "<script>window.opener.postMessage({error:'No family selected'},'*');window.close();</script>";
            return Content(html, "text/html");
        }
        await _gmailLink.LinkAsync(familyId, result.Principal, access, refresh, expires, ct);
        await HttpContext.SignOutAsync("GoogleTemp");
        var html = "<script>window.opener.postMessage({gmail:'linked'},'*');window.close();</script>";
        return Content(html, "text/html");
    }
}
