using DocumentManager.Application.Auth;

namespace DocumentManager.Application.Interfaces;

public interface IAuthService
{
    Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<LoginResponse> VerifyTwoFactorAsync(VerifyTwoFactorRequest request, CancellationToken cancellationToken = default);
    Task<RefreshTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default);
    Task<AuthenticatedUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default);
    Task<SetupTwoFactorResponse> SetupTwoFactorAsync(CancellationToken cancellationToken = default);
    Task ConfirmTwoFactorAsync(ConfirmTwoFactorRequest request, CancellationToken cancellationToken = default);
    Task DisableTwoFactorAsync(DisableTwoFactorRequest request, CancellationToken cancellationToken = default);
}
