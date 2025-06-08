using Google.Apis.Auth;
using Microsoft.AspNetCore.Authentication;

namespace Famoria.Api.Controllers;

public class AuthController : CustomControllerBase
{
    private readonly ISignInService _signIn;
    private readonly IJwtValidator<GoogleJsonWebSignature.Payload> _validator;

    public AuthController(IMediator mediator, ISignInService signIn, IJwtValidator<GoogleJsonWebSignature.Payload> validator) : base(mediator)
    {
        _signIn = signIn;
        _validator = validator;
    }

    [HttpGet("signin/google")]
    public IResult SignInGoogle([FromQuery] string returnUrl)
    {
        var props = new AuthenticationProperties
        {
            RedirectUri = $"auth/signin/google/callback?returnUrl={returnUrl}",
            AllowRefresh = true,
            IsPersistent = true
        };

        return Results.Challenge(props, ["GoogleSignIn"]);
    }

    [HttpGet("signin/google/callback")]
    public async Task<IActionResult> SignInGoogleCallback([FromQuery] string returnUrl, CancellationToken cancellationToken)
    {

        var result = await HttpContext.AuthenticateAsync("GoogleSignIn");

        if (!result.Succeeded || result.Principal is null)
        {
            return Content(
                "<script>window.opener.postMessage({error:'Authentication failed'},'*');window.close();</script>",
                "text/html");
        }

        var idToken = result.Properties.GetTokenValue("id_token");
        if (string.IsNullOrEmpty(idToken))
        {
            return Content(
                "<script>window.opener.postMessage({error:'Missing ID token'},'*');window.close();</script>",
                "text/html");
        }
        var payload = await _validator.ValidateAsync(idToken);
        if (!payload.EmailVerified)
        {
            return Content(
                "<script>window.opener.postMessage({error:'Unverified email'},'*');window.close();</script>",
                "text/html");
        }

        var token = await _signIn.SignInAsync("Google", result.Principal, cancellationToken);
        await HttpContext.SignOutAsync("GoogleTemp");
        var html = $"<script>window.opener.postMessage({{token:'{token}'}},'*');window.close();</script>";

        return Content(html, "text/html");
        //return Results.Redirect(returnUrl);
    }
}
