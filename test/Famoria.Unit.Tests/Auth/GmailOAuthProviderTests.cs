using System.IdentityModel.Tokens.Jwt;
using Famoria.Application.Services.Integrations;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using RichardSzalay.MockHttp;

namespace Famoria.Unit.Tests.Auth;

public class GmailOAuthProviderTests
{
    private static GoogleAuthSettings TestSettings => new()
    {
        ClientId = "test-client-id",
        ClientSecret = "test-secret",
        RedirectUri = "https://localhost/callback",
        Scopes = new[] { "https://mail.google.com/" }
    };

    [Fact]
    public void BuildConsentUrl_ShouldContainScopesAndRedirect()
    {
        var options = Mock.Of<IOptionsMonitor<GoogleAuthSettings>>(o => o.CurrentValue == TestSettings);
        var provider = new GmailOAuthProvider(
            new Mock<IHttpClientFactory>().Object,
            options,
            Mock.Of<ILogger<GmailOAuthProvider>>());
        var url = provider.BuildConsentUrl("state123", "user@gmail.com");
        url.Should().Contain("client_id=test-client-id");
        url.Should().Contain("redirect_uri=https%3A%2F%2Flocalhost%2Fcallback");
        url.Should().Contain("scope=https%3A%2F%2Fmail.google.com%2F");
        url.Should().Contain("state=state123");
        url.Should().Contain("access_type=offline");
    }

    [Fact]
    public async Task ExchangeCodeAsync_ShouldReturnTokenResult_WithEmailParsedFromIdToken()
    {
        var jwt = TestJwtGenerator.CreateJwt("user@gmail.com");
        var mockHttp = new MockHttpMessageHandler();
        mockHttp.When("https://oauth2.googleapis.com/token")
            .Respond("application/json", $"{{\"access_token\":\"ya29.test\",\"refresh_token\":\"refresh\",\"expires_in\":3600,\"id_token\":\"{jwt}\"}}");
        var clientFactory = new Mock<IHttpClientFactory>();
        clientFactory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(mockHttp.ToHttpClient());
        var options = Mock.Of<IOptionsMonitor<GoogleAuthSettings>>(o => o.CurrentValue == TestSettings);
        var provider = new GmailOAuthProvider(clientFactory.Object, options, Mock.Of<ILogger<GmailOAuthProvider>>());
        var result = await provider.ExchangeCodeAsync("abc", CancellationToken.None);
        result.AccessToken.Should().Be("ya29.test");
        result.RefreshToken.Should().Be("refresh");
        result.ExpiresInSeconds.Should().Be(3600);
        result.UserEmail.Should().Be("user@gmail.com");
    }
}

public static class TestJwtGenerator
{
    public static string CreateJwt(string email)
    {
        var handler = new JwtSecurityTokenHandler();
        var token = new JwtSecurityToken(
            claims: new[] { new System.Security.Claims.Claim("email", email) }
        );
        return handler.WriteToken(token);
    }
}
