using DocumentManager.Application.Auth;
using DocumentManager.Domain.Entities;

namespace DocumentManager.Application.Interfaces;

public interface ITokenService
{
    Task<TokenIssueResult> IssueTokensAsync(ApplicationUser user, IReadOnlyCollection<string> roles, string? existingFamilyId, CancellationToken cancellationToken = default);
    string CreateTwoFactorToken(ApplicationUser user);
    string ValidateTwoFactorToken(string token);
    string HashRefreshToken(string refreshToken);
}

public sealed record TokenIssueResult(
    string AccessToken,
    DateTimeOffset AccessTokenExpiresAt,
    string RefreshToken,
    DateTimeOffset RefreshTokenExpiresAt,
    string RefreshTokenHash,
    string FamilyId);
