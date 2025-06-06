using Famoria.Application.Interfaces;
using Famoria.Application.Services;
using Famoria.Application.Services.Integrations;
using FluentAssertions;
using Google.Apis.Auth;
using Microsoft.Extensions.Options;
using Moq;

namespace Famoria.Unit.Tests.Auth;

public class GoogleOAuthHelperTests
{
    private static GoogleAuthSettings TestSettings => new()
    {
        ClientId = "test-client-id",
        ClientSecret = "test-secret",
        RedirectUri = "https://localhost/callback",
        Scopes = new[] { "https://www.googleapis.com/auth/userinfo.email", "https://www.googleapis.com/auth/userinfo.profile" }
    };

    [Fact]
    public void BuildAuthUrl_ShouldContainScopesAndRedirect()
    {
        var options = Mock.Of<IOptionsMonitor<GoogleAuthSettings>>(o => o.CurrentValue == TestSettings);
        var mockOAuthProvider = new Mock<IMailOAuthProvider>();
        var helper = new GoogleOAuthHelper(new HttpClient(), options, mockOAuthProvider.Object);
        
        var url = helper.BuildAuthUrl("state123");
        
        url.Should().Contain("client_id=test-client-id");
        url.Should().Contain("redirect_uri=https%3A%2F%2Flocalhost%2Fcallback");
        // Fix: Use %20 (URL-encoded space) instead of + as the separator
        url.Should().Contain("scope=https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fuserinfo.email%20https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fuserinfo.profile");
        url.Should().Contain("state=state123");
        url.Should().Contain("prompt=select_account");
    }

    // Note: We can't easily test ExchangeCodeAsync because it calls the static method
    // GoogleJsonWebSignature.ValidateAsync, which requires special mocking techniques.
    // In a real project, you might use a library like Fody or refactor the code to be more testable.
}
