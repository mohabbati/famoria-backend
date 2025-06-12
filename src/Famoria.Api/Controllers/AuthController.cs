using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.IdentityModel.Tokens.Jwt;

namespace Famoria.Api.Controllers;

public class AuthController : CustomControllerBase
{
    private readonly ISignInService _signIn;
    private readonly IConfiguration _config;
    private readonly IJwtService _jwt;

    public AuthController(IMediator mediator,
                          ISignInService signIn,
                          IConfiguration config,
                          IJwtService jwt) : base(mediator)
    {
        _signIn = signIn;
        _config = config;
        _jwt = jwt;
    }

    [HttpGet("signin/google")]
    public IResult SignInGoogle([FromQuery] string returnUrl)
    {
        var safe = UrlHelper.GetReturnUrl(_config, returnUrl);
        var props = new AuthenticationProperties
        {
            RedirectUri = $"auth/signin/google/callback?returnUrl={safe}",
            AllowRefresh = true,
            IsPersistent = true
        };

        return Results.Challenge(props, ["Google"]);
    }

    [HttpGet("signin/google/callback")]
    public async Task<IActionResult> SignInGoogleCallback([FromQuery] string returnUrl, CancellationToken cancellationToken)
    {
        var origin = UrlHelper.GetOrigin(_config, returnUrl);
        var result = await HttpContext.AuthenticateAsync("Google");

        if (!result.Succeeded || result.Principal is null)
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'Authentication failed'}},'{origin}');window.close();</script>",
                "text/html");
        }

        var email = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        if (string.IsNullOrEmpty(email))
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'Email not found in claims'}},'{origin}');window.close();</script>",
                "text/html");
        }
        
        var token = await _signIn.SignInAsync(result.Principal, cancellationToken);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        Response.Cookies.Append(
            "ACCESS_TOKEN",
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddHours(12)
            });

        var html = $"<script>window.opener.postMessage({{email:'{email}',success:true}},'{origin}');window.close();</script>";

        return Content(html, "text/html");
    }

    [HttpGet("signin/microsoft")]
    public IResult SignInMicrosoft([FromQuery] string returnUrl)
    {
        var safe = UrlHelper.GetReturnUrl(_config, returnUrl);
        var props = new AuthenticationProperties
        {
            RedirectUri = $"auth/signin/microsoft/callback?returnUrl={safe}",
            AllowRefresh = true,
            IsPersistent = true
        };

        return Results.Challenge(props, ["Microsoft"]);
    }

    [HttpGet("signin/microsoft/callback")]
    public async Task<IActionResult> SignInMicrosoftCallback([FromQuery] string returnUrl, CancellationToken cancellationToken)
    {
        var origin = UrlHelper.GetOrigin(_config, returnUrl);

        var result = await HttpContext.AuthenticateAsync("Microsoft");

        if (!result.Succeeded || result.Principal is null)
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'Authentication failed'}},'{origin}');window.close();</script>",
                "text/html");
        }

        var token = await _signIn.SignInAsync(result.Principal, cancellationToken);
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        Response.Cookies.Append(
            "ACCESS_TOKEN",
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddHours(12)
            });

        var email = result.Principal.FindFirst(System.Security.Claims.ClaimTypes.Email)?.Value;
        var html = $"<script>window.opener.postMessage({{email:'{email}',success:true}},'{origin}');window.close();</script>";

        return Content(html, "text/html");
    }

    [Authorize]
    [HttpPost("signout")]
    public IActionResult SignOutUser()
    {
        Response.Cookies.Delete("ACCESS_TOKEN");
        return Ok(new { signedOut = true });
    }

    [Authorize]
    [HttpPost("refresh")]
    public IActionResult RefreshToken()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
        var familyId = User.FindFirst("family_id")?.Value;
        if (sub is null || email is null)
            return Unauthorized();

        var token = _jwt.Sign(sub, email, familyId);
        Response.Cookies.Append(
            "ACCESS_TOKEN",
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.None,
                Expires = DateTimeOffset.UtcNow.AddHours(12)
            });

        return Ok(new { refreshed = true });
    }

    [Authorize]
    [HttpGet("bff/user")]
    public IActionResult GetCurrentUser()
    {
        var sub = User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var email = User.FindFirst(JwtRegisteredClaimNames.Email)?.Value;
        var familyId = User.FindFirst("family_id")?.Value;
        if (sub is null || email is null)
            return Unauthorized();
        return Ok(new { sub, email, familyId });
    }
}
