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
        var mockJwtValidator = new Mock<IGoogleJwtValidator>();
        
        var helper = new GoogleOAuthHelper(new HttpClient(), options, mockOAuthProvider.Object, mockJwtValidator.Object);
        
        var url = helper.BuildAuthUrl("state123");
        
        url.Should().Contain("client_id=test-client-id");
        url.Should().Contain("redirect_uri=https%3A%2F%2Flocalhost%2Fcallback");
        // Fix: Use %20 (URL-encoded space) instead of + as the separator
        url.Should().Contain("scope=https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fuserinfo.email%20https%3A%2F%2Fwww.googleapis.com%2Fauth%2Fuserinfo.profile");
        url.Should().Contain("state=state123");
        url.Should().Contain("prompt=select_account");
    }

    [Fact]
    public async Task ExchangeCodeAsync_ShouldReturnValidatedPayload()
    {
        // Arrange
        var options = Mock.Of<IOptionsMonitor<GoogleAuthSettings>>(o => o.CurrentValue == TestSettings);
        var mockOAuthProvider = new Mock<IMailOAuthProvider>();
        var mockJwtValidator = new Mock<IGoogleJwtValidator>();
        
        var authCode = "test-auth-code";
        var idToken = "test-id-token";
        var expectedPayload = new GoogleJsonWebSignature.Payload
        {
            Email = "test@example.com",
            Name = "Test User",
            Subject = "12345",
            EmailVerified = true
        };
        
        // Setup the mock OAuth provider to return a token result with our test ID token
        mockOAuthProvider
            .Setup(p => p.ExchangeCodeAsync(authCode, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new TokenResult("access-token", "refresh-token", 3600, "test@example.com", idToken));
        
        // Setup the mock JWT validator to return our expected payload
        mockJwtValidator
            .Setup(v => v.ValidateAsync(idToken))
            .ReturnsAsync(expectedPayload);
        
        var helper = new GoogleOAuthHelper(new HttpClient(), options, mockOAuthProvider.Object, mockJwtValidator.Object);
        
        // Act
        var result = await helper.ExchangeCodeAsync(authCode, CancellationToken.None);
        
        // Assert
        result.Should().NotBeNull();
        result.Should().BeSameAs(expectedPayload);
        result.Email.Should().Be("test@example.com");
        result.Name.Should().Be("Test User");
        result.Subject.Should().Be("12345");
        
        // Verify that the methods were called with the expected parameters
        mockOAuthProvider.Verify(p => p.ExchangeCodeAsync(authCode, It.IsAny<CancellationToken>()), Times.Once);
        mockJwtValidator.Verify(v => v.ValidateAsync(idToken), Times.Once);
    }
}
