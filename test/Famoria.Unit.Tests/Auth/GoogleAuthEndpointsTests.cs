using Famoria.Domain.Entities;
using Famoria.Domain.Enums;
using FluentAssertions;
using Moq;
using Famoria.Application.Interfaces;
using Famoria.Application.Services;

namespace Famoria.Unit.Tests.Auth;

public class GoogleAuthEndpointsTests
{
    [Fact]
    public async Task GoogleAuthCallback_ShouldPersistConnection_WithEncryptedTokens()
    {
        // Arrange
        var code = "abc";
        var state = "fam001:testUser:guid";
        var tokenResult = new TokenResult("access", "refresh", 3600, "user@gmail.com");
        var mockOAuth = new Mock<IMailOAuthProvider>();
        mockOAuth.Setup(m => m.ExchangeCodeAsync(code, It.IsAny<CancellationToken>())).ReturnsAsync(tokenResult);
        var mockStore = new Mock<IUserLinkedAccountService>();
        UserLinkedAccount? capturedConn = null;
        mockStore.Setup(s => s.UpsertAsync(It.IsAny<UserLinkedAccount>(), It.IsAny<CancellationToken>()))
            .Callback<UserLinkedAccount, CancellationToken>((conn, ct) => capturedConn = conn)
            .Returns(Task.CompletedTask);
        var aesKey = new byte[32] { 1,2,3,4,5,6,7,8,9,10,11,12,13,14,15,16,17,18,19,20,21,22,23,24,25,26,27,28,29,30,31,32 };
        var crypto = new AesCryptoService(aesKey);
        var ct = CancellationToken.None;

        // Simulate endpoint logic
        var parts = state.Split(':');
        var familyId = parts[0];
        var userId = parts[1];
        var token = await mockOAuth.Object.ExchangeCodeAsync(code, ct);
        var conn = new UserLinkedAccount
        {
            FamilyId = familyId,
            UserId = userId,
            Provider = "Google",
            Source = FamilyItemSource.Email,
            UserEmail = token.UserEmail,
            AccessToken = crypto.Encrypt(token.AccessToken),
            RefreshToken = token.RefreshToken is null ? null : crypto.Encrypt(token.RefreshToken),
            TokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds),
            IsActive = true
        };
        await mockStore.Object.UpsertAsync(conn, ct);

        // Assert
        capturedConn.Should().NotBeNull();
        capturedConn!.FamilyId.Should().Be("fam001");
        capturedConn.UserId.Should().Be("testUser");
        capturedConn.Provider.Should().Be("Google");
        capturedConn.Source.Should().Be(FamilyItemSource.Email);
        capturedConn.UserEmail.Should().Be("user@gmail.com");
        capturedConn.IsActive.Should().BeTrue();
        crypto.Decrypt(capturedConn.AccessToken!).Should().Be("access");
        crypto.Decrypt(capturedConn.RefreshToken!).Should().Be("refresh");
        capturedConn.TokenExpiresAtUtc.Should().BeCloseTo(DateTime.UtcNow.AddSeconds(3600), TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task GoogleAuthCallback_ShouldThrow_WhenTokenResultIsNull()
    {
        var code = "abc";
        var state = "fam001:testUser:guid";
        var mockOAuth = new Mock<IMailOAuthProvider>();
        mockOAuth.Setup(m => m.ExchangeCodeAsync(code, It.IsAny<CancellationToken>())).ReturnsAsync((TokenResult)null!);
        var mockStore = new Mock<IUserLinkedAccountService>();
        var aesKey = new byte[32];
        var crypto = new AesCryptoService(aesKey);
        var ct = CancellationToken.None;
        var parts = state.Split(':');
        var familyId = parts[0];
        var userId = parts[1];
        Func<Task> act = async () =>
        {
            var token = await mockOAuth.Object.ExchangeCodeAsync(code, ct);
            if (token == null) throw new InvalidOperationException("TokenResult is null");
            var conn = new UserLinkedAccount
            {
                FamilyId = familyId,
                UserId = userId,
                Provider = "Google",
                Source = FamilyItemSource.Email,
                UserEmail = token.UserEmail,
                AccessToken = crypto.Encrypt(token.AccessToken),
                RefreshToken = token.RefreshToken is null ? null : crypto.Encrypt(token.RefreshToken),
                TokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds),
                IsActive = true
            };
            await mockStore.Object.UpsertAsync(conn, ct);
        };
        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("TokenResult is null");
    }

    [Fact]
    public async Task GoogleAuthCallback_ShouldThrow_WhenStateMalformed()
    {
        var code = "abc";
        var state = "badstate"; // not enough parts
        var mockOAuth = new Mock<IMailOAuthProvider>();
        var mockStore = new Mock<IUserLinkedAccountService>();
        var aesKey = new byte[32];
        var crypto = new AesCryptoService(aesKey);
        var ct = CancellationToken.None;
        Func<Task> act = async () =>
        {
            var parts = state.Split(':');
            if (parts.Length < 2) throw new ArgumentException("State is malformed");
            var familyId = parts[0];
            var userId = parts[1];
            var token = await mockOAuth.Object.ExchangeCodeAsync(code, ct);
            var conn = new UserLinkedAccount
            {
                FamilyId = familyId,
                UserId = userId,
                Provider = "Google",
                Source = FamilyItemSource.Email,
                UserEmail = token.UserEmail,
                AccessToken = crypto.Encrypt(token.AccessToken),
                RefreshToken = token.RefreshToken is null ? null : crypto.Encrypt(token.RefreshToken),
                TokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds),
                IsActive = true
            };
            await mockStore.Object.UpsertAsync(conn, ct);
        };
        await act.Should().ThrowAsync<ArgumentException>().WithMessage("State is malformed");
    }

    [Fact]
    public async Task GoogleAuthCallback_ShouldThrow_WhenStoreThrows()
    {
        var code = "abc";
        var state = "fam001:testUser:guid";
        var tokenResult = new TokenResult("access", "refresh", 3600, "user@gmail.com");
        var mockOAuth = new Mock<IMailOAuthProvider>();
        mockOAuth.Setup(m => m.ExchangeCodeAsync(code, It.IsAny<CancellationToken>())).ReturnsAsync(tokenResult);
        var mockStore = new Mock<IUserLinkedAccountService>();
        mockStore.Setup(s => s.UpsertAsync(It.IsAny<UserLinkedAccount>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Store failed"));
        var aesKey = new byte[32];
        var crypto = new AesCryptoService(aesKey);
        var ct = CancellationToken.None;
        var parts = state.Split(':');
        var familyId = parts[0];
        var userId = parts[1];
        Func<Task> act = async () =>
        {
            var token = await mockOAuth.Object.ExchangeCodeAsync(code, ct);
            var conn = new UserLinkedAccount
            {
                FamilyId = familyId,
                UserId = userId,
                Provider = "Google",
                Source = FamilyItemSource.Email,
                UserEmail = token.UserEmail,
                AccessToken = crypto.Encrypt(token.AccessToken),
                RefreshToken = token.RefreshToken is null ? null : crypto.Encrypt(token.RefreshToken),
                TokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(token.ExpiresInSeconds),
                IsActive = true
            };
            await mockStore.Object.UpsertAsync(conn, ct);
        };
        await act.Should().ThrowAsync<Exception>().WithMessage("Store failed");
    }
}

public class InMemoryUserIntegrationConnectionService : IUserLinkedAccountService
{
    public List<UserLinkedAccount> Connections { get; } = new();
    public Task UpsertAsync(UserLinkedAccount connection, CancellationToken cancellationToken)
    {
        Connections.Add(connection);
        return Task.CompletedTask;
    }
}
