using System.Text.Json.Serialization;

namespace DocumentManager.Application.Auth;

public sealed record LoginRequest(string UsernameOrEmail, string Password);

public sealed record VerifyTwoFactorRequest(string TwoFactorToken, string Code);

public sealed record RefreshTokenResponse(string AccessToken, DateTimeOffset AccessTokenExpiresAt, AuthenticatedUserDto User)
{
    [JsonIgnore]
    public string? RefreshToken { get; init; }

    [JsonIgnore]
    public DateTimeOffset? RefreshTokenExpiresAt { get; init; }
}

public sealed record LoginResponse(
    bool RequiresTwoFactor,
    string? TwoFactorToken,
    string? AccessToken,
    DateTimeOffset? AccessTokenExpiresAt,
    AuthenticatedUserDto? User)
{
    [JsonIgnore]
    public string? RefreshToken { get; init; }

    [JsonIgnore]
    public DateTimeOffset? RefreshTokenExpiresAt { get; init; }
}

public sealed record AuthenticatedUserDto(
    string Id,
    string UserName,
    string Email,
    string FirstName,
    string LastName,
    bool TwoFactorEnabled,
    IReadOnlyCollection<string> Roles);

public sealed record SetupTwoFactorResponse(string ManualEntryKey, string OtpAuthUri);

public sealed record ConfirmTwoFactorRequest(string Code);

public sealed record DisableTwoFactorRequest(string? Code);
