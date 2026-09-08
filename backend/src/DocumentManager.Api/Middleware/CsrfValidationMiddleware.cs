using DocumentManager.Api.Configuration;

namespace DocumentManager.Api.Middleware;

public sealed class CsrfValidationMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context)
    {
        if (RequiresCsrf(context.Request))
        {
            var cookieToken = context.Request.Cookies[CookieNames.CsrfToken];
            var headerToken = context.Request.Headers[CookieNames.CsrfHeader].FirstOrDefault();

            if (string.IsNullOrWhiteSpace(cookieToken) ||
                string.IsNullOrWhiteSpace(headerToken) ||
                !string.Equals(cookieToken, headerToken, StringComparison.Ordinal))
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                await context.Response.WriteAsJsonAsync(new
                {
                    title = "CSRF validation failed",
                    status = StatusCodes.Status400BadRequest,
                    detail = "La solicitud no incluye un token CSRF valido.",
                    correlationId = context.Items["CorrelationId"]
                });
                return;
            }
        }

        await next(context);
    }

    private static bool RequiresCsrf(HttpRequest request) =>
        HttpMethods.IsPost(request.Method) &&
        (request.Path.Equals("/api/auth/refresh", StringComparison.OrdinalIgnoreCase) ||
         request.Path.Equals("/api/auth/logout", StringComparison.OrdinalIgnoreCase));
}
