using DocumentManager.Api.Configuration;
using DocumentManager.Application.Auth;
using DocumentManager.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;

namespace DocumentManager.Api.Controllers;

[ApiController]
[Route("api/auth")]
public sealed class AuthController(IAuthService authService, IWebHostEnvironment environment) : ControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(LoginRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.LoginAsync(request, cancellationToken);
        AppendAuthCookies(response.RefreshToken, response.RefreshTokenExpiresAt);
        return Ok(response);
    }

    [HttpPost("verify-2fa")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> VerifyTwoFactor(VerifyTwoFactorRequest request, CancellationToken cancellationToken)
    {
        var response = await authService.VerifyTwoFactorAsync(request, cancellationToken);
        AppendAuthCookies(response.RefreshToken, response.RefreshTokenExpiresAt);
        return Ok(response);
    }

    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("refresh")]
    public async Task<IActionResult> Refresh(CancellationToken cancellationToken)
    {
        var response = await authService.RefreshAsync(Request.Cookies[CookieNames.RefreshToken] ?? string.Empty, cancellationToken);
        AppendAuthCookies(response.RefreshToken, response.RefreshTokenExpiresAt);
        return Ok(response);
    }

    [HttpPost("logout")]
    [Authorize]
    [EnableRateLimiting("refresh")]
    public async Task<IActionResult> Logout(CancellationToken cancellationToken)
    {
        await authService.LogoutAsync(Request.Cookies[CookieNames.RefreshToken], cancellationToken);
        Response.Cookies.Delete(CookieNames.RefreshToken);
        Response.Cookies.Delete(CookieNames.CsrfToken);
        return NoContent();
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<IActionResult> Me(CancellationToken cancellationToken)
    {
        return Ok(await authService.GetCurrentUserAsync(cancellationToken));
    }

    [HttpPost("2fa/setup")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> SetupTwoFactor(CancellationToken cancellationToken)
    {
        return Ok(await authService.SetupTwoFactorAsync(cancellationToken));
    }

    [HttpPost("2fa/confirm")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> ConfirmTwoFactor(ConfirmTwoFactorRequest request, CancellationToken cancellationToken)
    {
        await authService.ConfirmTwoFactorAsync(request, cancellationToken);
        return NoContent();
    }

    [HttpPost("2fa/disable")]
    [Authorize]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> DisableTwoFactor(DisableTwoFactorRequest request, CancellationToken cancellationToken)
    {
        await authService.DisableTwoFactorAsync(request, cancellationToken);
        return NoContent();
    }

    private void AppendAuthCookies(string? refreshToken, DateTimeOffset? expiresAt)
    {
        if (string.IsNullOrWhiteSpace(refreshToken) || expiresAt is null)
        {
            return;
        }

        var secure = !environment.IsDevelopment();
        Response.Cookies.Append(CookieNames.RefreshToken, refreshToken, new CookieOptions
        {
            HttpOnly = true,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt,
            Path = "/"
        });

        Response.Cookies.Append(CookieNames.CsrfToken, Guid.NewGuid().ToString("N"), new CookieOptions
        {
            HttpOnly = false,
            Secure = secure,
            SameSite = SameSiteMode.Strict,
            Expires = expiresAt,
            Path = "/"
        });
    }
}
