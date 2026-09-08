using System.Text;
using DocumentManager.Application.Auth;
using DocumentManager.Application.Interfaces;
using DocumentManager.Domain.Constants;
using DocumentManager.Domain.Entities;
using DocumentManager.Domain.Exceptions;
using DocumentManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DocumentManager.Infrastructure.Authentication;

public sealed class AuthService(
    UserManager<ApplicationUser> userManager,
    ITokenService tokenService,
    ICurrentUserService currentUser,
    IAuditService auditService,
    ApplicationDbContext dbContext) : IAuthService
{
    public async Task<LoginResponse> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
    {
        var user = await FindByUsernameOrEmailAsync(request.UsernameOrEmail);
        if (user is null)
        {
            await AuditAndSaveAsync(AuditActions.LoginFailed, "ApplicationUser", null, "Intento de login con usuario inexistente.", cancellationToken);
            throw new UnauthorizedAppException("Credenciales invalidas.");
        }

        if (!user.IsActive)
        {
            await AuditAndSaveAsync(AuditActions.LoginFailed, nameof(ApplicationUser), user.Id, "Intento de login con usuario desactivado.", cancellationToken);
            throw new UnauthorizedAppException("Credenciales invalidas.");
        }

        if (await userManager.IsLockedOutAsync(user))
        {
            await AuditAndSaveAsync(AuditActions.LoginFailed, nameof(ApplicationUser), user.Id, "Intento de login con cuenta bloqueada temporalmente.", cancellationToken);
            throw new UnauthorizedAppException("La cuenta esta bloqueada temporalmente.");
        }

        var passwordValid = await userManager.CheckPasswordAsync(user, request.Password);
        if (!passwordValid)
        {
            await userManager.AccessFailedAsync(user);
            await AuditAndSaveAsync(AuditActions.LoginFailed, nameof(ApplicationUser), user.Id, "Intento de login fallido.", cancellationToken);
            throw new UnauthorizedAppException("Credenciales invalidas.");
        }

        await userManager.ResetAccessFailedCountAsync(user);
        if (await userManager.GetTwoFactorEnabledAsync(user))
        {
            await AuditAndSaveAsync(AuditActions.TwoFactorSetup, nameof(ApplicationUser), user.Id, "Credenciales validas; se requiere doble factor.", cancellationToken);
            return new LoginResponse(true, tokenService.CreateTwoFactorToken(user), null, null, null);
        }

        return await IssueLoginResponseAsync(user, AuditActions.LoginSuccess, "Login exitoso.", cancellationToken);
    }

    public async Task<LoginResponse> VerifyTwoFactorAsync(VerifyTwoFactorRequest request, CancellationToken cancellationToken = default)
    {
        var userId = tokenService.ValidateTwoFactorToken(request.TwoFactorToken);
        var user = await userManager.FindByIdAsync(userId) ?? throw new UnauthorizedAppException("Token de doble factor invalido.");
        if (!user.IsActive)
        {
            throw new UnauthorizedAppException("La cuenta no esta activa.");
        }

        var valid = await userManager.VerifyTwoFactorTokenAsync(
            user,
            TokenOptions.DefaultAuthenticatorProvider,
            NormalizeCode(request.Code));

        if (!valid)
        {
            await AuditAndSaveAsync(AuditActions.TwoFactorFailed, nameof(ApplicationUser), user.Id, "Codigo 2FA invalido.", cancellationToken);
            throw new UnauthorizedAppException("Codigo de doble factor invalido.");
        }

        await AuditAndSaveAsync(AuditActions.TwoFactorSuccess, nameof(ApplicationUser), user.Id, "Codigo 2FA validado correctamente.", cancellationToken);
        return await IssueLoginResponseAsync(user, AuditActions.LoginSuccess, "Login exitoso con doble factor.", cancellationToken);
    }

    public async Task<RefreshTokenResponse> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(refreshToken))
        {
            throw new UnauthorizedAppException();
        }

        var hash = tokenService.HashRefreshToken(refreshToken);
        var storedToken = await dbContext.RefreshTokens
            .Include(token => token.User)
            .FirstOrDefaultAsync(token => token.TokenHash == hash, cancellationToken)
            ?? throw new UnauthorizedAppException();

        var now = DateTimeOffset.UtcNow;
        if (!storedToken.IsActive(now))
        {
            await RevokeFamilyAsync(storedToken.FamilyId, "Refresh token reutilizado o expirado.", cancellationToken);
            throw new UnauthorizedAppException("Refresh token invalido.");
        }

        var user = storedToken.User ?? throw new UnauthorizedAppException();
        if (!user.IsActive)
        {
            await RevokeFamilyAsync(storedToken.FamilyId, "Refresh token rechazado porque el usuario esta desactivado.", cancellationToken);
            throw new UnauthorizedAppException();
        }

        var roles = await userManager.GetRolesAsync(user);
        var issued = await tokenService.IssueTokensAsync(user, roles.ToArray(), storedToken.FamilyId, cancellationToken);
        var newToken = CreateRefreshToken(user.Id, issued);

        storedToken.RevokedAt = now;
        storedToken.RevokedByIp = currentUser.IpAddress;
        storedToken.ReplacedByTokenId = newToken.Id;
        dbContext.RefreshTokens.Add(newToken);

        await auditService.RecordAsync(AuditActions.TokenRefresh, nameof(RefreshToken), storedToken.Id.ToString(), "Refresh token rotado correctamente.", null, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new RefreshTokenResponse(issued.AccessToken, issued.AccessTokenExpiresAt, await ToUserDtoAsync(user))
        {
            RefreshToken = issued.RefreshToken,
            RefreshTokenExpiresAt = issued.RefreshTokenExpiresAt
        };
    }

    public async Task LogoutAsync(string? refreshToken, CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(refreshToken))
        {
            var hash = tokenService.HashRefreshToken(refreshToken);
            var storedToken = await dbContext.RefreshTokens.FirstOrDefaultAsync(token => token.TokenHash == hash, cancellationToken);
            if (storedToken is not null && storedToken.RevokedAt is null)
            {
                storedToken.RevokedAt = DateTimeOffset.UtcNow;
                storedToken.RevokedByIp = currentUser.IpAddress;
            }
        }

        await auditService.RecordAsync(AuditActions.Logout, "Session", currentUser.UserId, "Logout solicitado.", null, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<AuthenticatedUserDto> GetCurrentUserAsync(CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAppException();
        var user = await userManager.FindByIdAsync(userId) ?? throw new UnauthorizedAppException();
        if (!user.IsActive)
        {
            throw new UnauthorizedAppException();
        }

        return await ToUserDtoAsync(user);
    }

    public async Task<SetupTwoFactorResponse> SetupTwoFactorAsync(CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentApplicationUserAsync();
        var key = await userManager.GetAuthenticatorKeyAsync(user);
        if (string.IsNullOrWhiteSpace(key))
        {
            await userManager.ResetAuthenticatorKeyAsync(user);
            key = await userManager.GetAuthenticatorKeyAsync(user);
        }

        await auditService.RecordAsync(AuditActions.TwoFactorSetup, nameof(ApplicationUser), user.Id, "Se genero configuracion inicial de 2FA.", null, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        var issuer = "DocumentManager";
        var email = user.Email ?? user.UserName ?? user.Id;
        var otpUri = $"otpauth://totp/{Uri.EscapeDataString(issuer)}:{Uri.EscapeDataString(email)}?secret={key}&issuer={Uri.EscapeDataString(issuer)}&digits=6";
        return new SetupTwoFactorResponse(FormatAuthenticatorKey(key!), otpUri);
    }

    public async Task ConfirmTwoFactorAsync(ConfirmTwoFactorRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentApplicationUserAsync();
        var valid = await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, NormalizeCode(request.Code));
        if (!valid)
        {
            await AuditAndSaveAsync(AuditActions.TwoFactorFailed, nameof(ApplicationUser), user.Id, "Confirmacion 2FA fallida.", cancellationToken);
            throw new ValidationAppException("El codigo 2FA no es valido.");
        }

        await userManager.SetTwoFactorEnabledAsync(user, true);
        await AuditAndSaveAsync(AuditActions.TwoFactorEnabled, nameof(ApplicationUser), user.Id, "2FA activado correctamente.", cancellationToken);
    }

    public async Task DisableTwoFactorAsync(DisableTwoFactorRequest request, CancellationToken cancellationToken = default)
    {
        var user = await GetCurrentApplicationUserAsync();
        if (await userManager.GetTwoFactorEnabledAsync(user))
        {
            if (string.IsNullOrWhiteSpace(request.Code))
            {
                throw new ValidationAppException("Debe enviar el codigo actual para desactivar 2FA.");
            }

            var valid = await userManager.VerifyTwoFactorTokenAsync(user, TokenOptions.DefaultAuthenticatorProvider, NormalizeCode(request.Code));
            if (!valid)
            {
                await AuditAndSaveAsync(AuditActions.TwoFactorFailed, nameof(ApplicationUser), user.Id, "Desactivacion 2FA fallida.", cancellationToken);
                throw new ValidationAppException("El codigo 2FA no es valido.");
            }
        }

        await userManager.SetTwoFactorEnabledAsync(user, false);
        await userManager.ResetAuthenticatorKeyAsync(user);
        await AuditAndSaveAsync(AuditActions.TwoFactorDisabled, nameof(ApplicationUser), user.Id, "2FA desactivado.", cancellationToken);
    }

    private async Task<LoginResponse> IssueLoginResponseAsync(ApplicationUser user, string auditAction, string auditDescription, CancellationToken cancellationToken)
    {
        var roles = await userManager.GetRolesAsync(user);
        var issued = await tokenService.IssueTokensAsync(user, roles.ToArray(), null, cancellationToken);
        dbContext.RefreshTokens.Add(CreateRefreshToken(user.Id, issued));
        user.LastLoginAt = DateTimeOffset.UtcNow;
        await userManager.UpdateAsync(user);
        await auditService.RecordAsync(auditAction, nameof(ApplicationUser), user.Id, auditDescription, null, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new LoginResponse(false, null, issued.AccessToken, issued.AccessTokenExpiresAt, await ToUserDtoAsync(user))
        {
            RefreshToken = issued.RefreshToken,
            RefreshTokenExpiresAt = issued.RefreshTokenExpiresAt
        };
    }

    private RefreshToken CreateRefreshToken(string userId, TokenIssueResult issued) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        TokenHash = issued.RefreshTokenHash,
        FamilyId = issued.FamilyId,
        CreatedAt = DateTimeOffset.UtcNow,
        ExpiresAt = issued.RefreshTokenExpiresAt,
        CreatedByIp = currentUser.IpAddress,
        UserAgent = currentUser.UserAgent
    };

    private async Task RevokeFamilyAsync(string familyId, string description, CancellationToken cancellationToken)
    {
        var tokens = await dbContext.RefreshTokens.Where(token => token.FamilyId == familyId && token.RevokedAt == null).ToListAsync(cancellationToken);
        foreach (var token in tokens)
        {
            token.RevokedAt = DateTimeOffset.UtcNow;
            token.RevokedByIp = currentUser.IpAddress;
        }

        await auditService.RecordAsync(AuditActions.TokenRevoked, nameof(RefreshToken), familyId, description, null, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<ApplicationUser?> FindByUsernameOrEmailAsync(string usernameOrEmail)
    {
        var normalized = usernameOrEmail.Trim();
        return normalized.Contains('@', StringComparison.Ordinal)
            ? await userManager.FindByEmailAsync(normalized)
            : await userManager.FindByNameAsync(normalized);
    }

    private async Task<ApplicationUser> GetCurrentApplicationUserAsync()
    {
        var userId = currentUser.UserId ?? throw new UnauthorizedAppException();
        var user = await userManager.FindByIdAsync(userId) ?? throw new UnauthorizedAppException();
        if (!user.IsActive)
        {
            throw new UnauthorizedAppException();
        }

        return user;
    }

    private async Task<AuthenticatedUserDto> ToUserDtoAsync(ApplicationUser user)
    {
        var roles = await userManager.GetRolesAsync(user);
        return new AuthenticatedUserDto(
            user.Id,
            user.UserName ?? string.Empty,
            user.Email ?? string.Empty,
            user.FirstName,
            user.LastName,
            user.TwoFactorEnabled,
            roles.ToArray());
    }

    private async Task AuditAndSaveAsync(string action, string entityType, string? entityId, string description, CancellationToken cancellationToken)
    {
        await auditService.RecordAsync(action, entityType, entityId, description, null, cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeCode(string code) => code.Replace(" ", string.Empty, StringComparison.Ordinal).Replace("-", string.Empty, StringComparison.Ordinal);

    private static string FormatAuthenticatorKey(string key)
    {
        var builder = new StringBuilder();
        for (var i = 0; i < key.Length; i++)
        {
            if (i > 0 && i % 4 == 0)
            {
                builder.Append(' ');
            }

            builder.Append(key[i]);
        }

        return builder.ToString().ToLowerInvariant();
    }
}
