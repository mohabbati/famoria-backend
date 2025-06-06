using Famoria.Application.Services.Auth;
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

    public AuthController(GoogleSignInService signIn, GmailLinkService gmailLink)
    {
        _signIn = signIn;
        _gmailLink = gmailLink;
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
            return BadRequest();
        var (token, familyId) = await _signIn.SignInAsync(result.Principal, ct);
        await HttpContext.SignOutAsync("GoogleTemp");
        var html = $"<script>window.opener.postMessage({{token:'{token}',familyId:'{familyId}'}},'*');window.close();</script>";
        return Content(html, "text/html");
    }

    [Authorize]
    [HttpGet("link/google-mail")]
    public IActionResult LinkGmail()
    {
        var props = new AuthenticationProperties { RedirectUri = "/link-google-mail" };
        return Challenge(props, "GoogleMailLink");
    }

    [Authorize]
    [HttpGet("/link-google-mail")]
    public async Task<IActionResult> LinkGmailCallback(CancellationToken ct)
    {
        var result = await HttpContext.AuthenticateAsync("GoogleTemp");
        if (!result.Succeeded || result.Principal is null)
            return BadRequest();
        var access = result.Properties.GetTokenValue("access_token")!;
        var refresh = result.Properties.GetTokenValue("refresh_token");
        var expires = int.TryParse(result.Properties.GetTokenValue("expires_in"), out var e) ? e : 0;
        var familyId = User.FindFirst("family_id")!.Value;
        await _gmailLink.LinkAsync(familyId, result.Principal, access, refresh, expires, ct);
        await HttpContext.SignOutAsync("GoogleTemp");
        return Ok("Gmail connected");
    }
}
