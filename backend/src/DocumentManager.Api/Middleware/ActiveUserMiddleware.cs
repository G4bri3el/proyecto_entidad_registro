using System.Security.Claims;
using DocumentManager.Domain.Entities;
using Microsoft.AspNetCore.Identity;

namespace DocumentManager.Api.Middleware;

public sealed class ActiveUserMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, UserManager<ApplicationUser> userManager)
    {
        if (context.User.Identity?.IsAuthenticated == true)
        {
            var userId = context.User.FindFirstValue(ClaimTypes.NameIdentifier) ?? context.User.FindFirstValue("sub");
            var tokenStamp = context.User.FindFirstValue("security_stamp");
            var user = string.IsNullOrWhiteSpace(userId) ? null : await userManager.FindByIdAsync(userId);

            if (user is null || !user.IsActive || !string.Equals(user.SecurityStamp, tokenStamp, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsJsonAsync(new
                {
                    title = "Unauthorized",
                    status = StatusCodes.Status401Unauthorized,
                    detail = "La sesion no es valida o ha expirado.",
                    correlationId = context.Items["CorrelationId"]
                });
                return;
            }
        }

        await next(context);
    }
}
