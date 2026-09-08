using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DocumentManager.Application.Configuration;
using DocumentManager.Application.Interfaces;
using DocumentManager.Domain.Entities;
using DocumentManager.Domain.Exceptions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace DocumentManager.Infrastructure.Authentication;

public sealed class TokenService(IOptions<JwtOptions> optionsAccessor) : ITokenService
{
    private readonly JwtOptions _options = optionsAccessor.Value;

    public Task<TokenIssueResult> IssueTokensAsync(ApplicationUser user, IReadOnlyCollection<string> roles, string? existingFamilyId, CancellationToken cancellationToken = default)
    {
        var now = DateTimeOffset.UtcNow;
        var accessExpires = now.AddMinutes(_options.AccessTokenMinutes);
        var refreshExpires = now.AddDays(_options.RefreshTokenDays);
        var familyId = string.IsNullOrWhiteSpace(existingFamilyId) ? Guid.NewGuid().ToString("N") : existingFamilyId;
        var refreshToken = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(64));

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(ClaimTypes.NameIdentifier, user.Id),
            new(ClaimTypes.Name, user.UserName ?? user.Email ?? user.Id),
            new(JwtRegisteredClaimNames.Email, user.Email ?? string.Empty),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N")),
            new("security_stamp", user.SecurityStamp ?? string.Empty)
        };

        claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
        var token = CreateJwt(claims, accessExpires);

        return Task.FromResult(new TokenIssueResult(
            token,
            accessExpires,
            refreshToken,
            refreshExpires,
            HashRefreshToken(refreshToken),
            familyId));
    }

    public string CreateTwoFactorToken(ApplicationUser user)
    {
        var expires = DateTimeOffset.UtcNow.AddMinutes(_options.TwoFactorTokenMinutes);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id),
            new Claim("purpose", "2fa"),
            new Claim("security_stamp", user.SecurityStamp ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        return CreateJwt(claims, expires);
    }

    public string ValidateTwoFactorToken(string token)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var principal = handler.ValidateToken(token, CreateValidationParameters(), out _);
            var purpose = principal.FindFirstValue("purpose");
            if (!string.Equals(purpose, "2fa", StringComparison.Ordinal))
            {
                throw new UnauthorizedAppException("El token de doble factor no es valido.");
            }

            return principal.FindFirstValue(JwtRegisteredClaimNames.Sub)
                ?? principal.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new UnauthorizedAppException("El token de doble factor no contiene usuario.");
        }
        catch (SecurityTokenException)
        {
            throw new UnauthorizedAppException("El token de doble factor no es valido.");
        }
    }

    public string HashRefreshToken(string refreshToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(refreshToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private string CreateJwt(IEnumerable<Claim> claims, DateTimeOffset expires)
    {
        var credentials = new SigningCredentials(GetSigningKey(), SecurityAlgorithms.HmacSha256);
        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Issuer = _options.Issuer,
            Audience = _options.Audience,
            Expires = expires.UtcDateTime,
            SigningCredentials = credentials
        };

        var handler = new JwtSecurityTokenHandler();
        return handler.WriteToken(handler.CreateToken(descriptor));
    }

    private TokenValidationParameters CreateValidationParameters() => new()
    {
        ValidateIssuer = true,
        ValidIssuer = _options.Issuer,
        ValidateAudience = true,
        ValidAudience = _options.Audience,
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = GetSigningKey(),
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromSeconds(30)
    };

    private SymmetricSecurityKey GetSigningKey()
    {
        if (string.IsNullOrWhiteSpace(_options.SigningKey) || _options.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey debe configurarse mediante user-secrets o variables de entorno y tener al menos 32 caracteres.");
        }

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_options.SigningKey));
    }
}
