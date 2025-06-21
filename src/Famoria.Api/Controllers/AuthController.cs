using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Famoria.Api.Controllers;

public class AuthController : CustomControllerBase
{
    private readonly IUserService _userService;
    private readonly IConfiguration _config;
    private readonly IJwtService _jwt;

    public AuthController(IMediator mediator,
                          IUserService signIn,
                          IConfiguration config,
                          IJwtService jwt) : base(mediator)
    {
        _userService = signIn;
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

        var email = result.Principal.GetClaim(ClaimTypes.Email);
        if (string.IsNullOrEmpty(email))
        {
            return Content(
                $"<script>window.opener.postMessage({{error:'Email not found in claims'}},'{origin}');window.close();</script>",
                "text/html");
        }

        var userDto = CreateUserDto(result.Principal);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (string.IsNullOrEmpty(userDto.Provider))
            throw new InvalidOperationException("issuer missing");
        if (string.IsNullOrEmpty(userDto.ProviderSub))
            throw new InvalidOperationException("subject missing");

        var token = await _userService.SignInAsync(userDto, cancellationToken);

        Response.AppendAccessToken(token);

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

        var userDto = CreateUserDto(result.Principal);

        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        if (string.IsNullOrEmpty(userDto.Provider))
            throw new InvalidOperationException("issuer missing");
        if (string.IsNullOrEmpty(userDto.ProviderSub))
            throw new InvalidOperationException("subject missing");

        var token = await _userService.SignInAsync(userDto, cancellationToken);

        Response.AppendAccessToken(token);

        var email = result.Principal.FindFirst(ClaimTypes.Email)?.Value;
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
    [HttpGet("bff/user")]
    public IActionResult GetCurrentUser()
    {
        var sub = User.GetClaim(ClaimTypes.NameIdentifier) ??
            throw new InvalidOperationException("subject missing");
        var email = User.GetClaim(ClaimTypes.Email) ??
            throw new InvalidOperationException("email missing");
        var familyId = User.GetClaim("family_id");

        return string.IsNullOrEmpty(sub) || string.IsNullOrEmpty(email)
            ? Unauthorized()
            : Ok(new { sub, email, familyId });
    }

    private FamoriaUserDto CreateUserDto(ClaimsPrincipal user)
    {
        var name = user.GetClaim(ClaimTypes.Name) ?? string.Empty;
        var firstName = user.GetClaim(ClaimTypes.GivenName) ?? string.Empty;
        var lastName = user.GetClaim(ClaimTypes.Surname) ?? string.Empty;
        var email = user.GetClaim(ClaimTypes.Email) ?? string.Empty;
        var iss = user.Claims.Select(c => c.Issuer).FirstOrDefault()?.ToLowerInvariant() ?? string.Empty;
        var sub = user.GetClaim(ClaimTypes.NameIdentifier) ?? string.Empty;
        return new FamoriaUserDto(name, firstName, lastName, email, iss, sub, []);
    }
}
