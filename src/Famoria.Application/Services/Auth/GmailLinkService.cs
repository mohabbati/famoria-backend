using Famoria.Application.Interfaces;
using Famoria.Domain.Common;
using Famoria.Domain.Entities;
using Famoria.Domain.Enums;
using System.Security.Claims;

namespace Famoria.Application.Services.Auth;

public class GmailLinkService
{
    private readonly IUserLinkedAccountService _accounts;
    private readonly IAesCryptoService _crypto;

    public GmailLinkService(IUserLinkedAccountService accounts, IAesCryptoService crypto)
    {
        _accounts = accounts;
        _crypto = crypto;
    }

    public async Task LinkAsync(string familyId, ClaimsPrincipal principal, string accessToken, string? refreshToken, int expiresInSeconds, CancellationToken cancellationToken = default)
    {
        var userId = principal.FindFirstValue("sub") ?? principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? throw new InvalidOperationException("sub missing");
        var email = principal.FindFirstValue(ClaimTypes.Email) ?? string.Empty;

        var conn = new UserLinkedAccount
        {
            FamilyId = familyId,
            UserId = userId,
            Provider = "Google",
            Source = FamilyItemSource.Email,
            UserEmail = email,
            AccessToken = _crypto.Encrypt(accessToken),
            RefreshToken = refreshToken is null ? null : _crypto.Encrypt(refreshToken),
            TokenExpiresAtUtc = DateTime.UtcNow.AddSeconds(expiresInSeconds),
            IsActive = true
        };
        await _accounts.UpsertAsync(conn, cancellationToken);
    }
}
