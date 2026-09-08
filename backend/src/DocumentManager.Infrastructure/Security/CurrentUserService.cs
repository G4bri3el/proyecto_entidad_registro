using System.Security.Claims;
using DocumentManager.Application.Interfaces;
using Microsoft.AspNetCore.Http;

namespace DocumentManager.Infrastructure.Security;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    private HttpContext? HttpContext => httpContextAccessor.HttpContext;

    public string? UserId => HttpContext?.User.FindFirstValue(ClaimTypes.NameIdentifier)
        ?? HttpContext?.User.FindFirstValue("sub");

    public string? UserName => HttpContext?.User.Identity?.Name
        ?? HttpContext?.User.FindFirstValue(ClaimTypes.Name);

    public string? IpAddress => HttpContext?.Connection.RemoteIpAddress?.ToString();
    public string? UserAgent => HttpContext?.Request.Headers.UserAgent.ToString();
    public string? HttpMethod => HttpContext?.Request.Method;
    public string? Endpoint => HttpContext?.Request.Path.Value;
    public int? StatusCode => HttpContext?.Response.StatusCode;
    public string CorrelationId => HttpContext?.Items["CorrelationId"]?.ToString()
        ?? HttpContext?.TraceIdentifier
        ?? Guid.NewGuid().ToString("N");
}
