using DocumentManager.Domain.Exceptions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DocumentManager.Api.Middleware;

public sealed class ExceptionHandlingMiddleware(RequestDelegate next, ILogger<ExceptionHandlingMiddleware> logger, IWebHostEnvironment environment)
{
    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception)
        {
            await HandleAsync(context, exception);
        }
    }

    private async Task HandleAsync(HttpContext context, Exception exception)
    {
        var correlationId = context.Items["CorrelationId"]?.ToString() ?? context.TraceIdentifier;
        var (statusCode, title, detail) = exception switch
        {
            AppException appException => (appException.StatusCode, appException.Title, appException.Message),
            DbUpdateConcurrencyException => (StatusCodes.Status409Conflict, "Conflict", "El recurso fue modificado por otro usuario."),
            BadHttpRequestException badRequest when badRequest.StatusCode == StatusCodes.Status413PayloadTooLarge => (StatusCodes.Status413PayloadTooLarge, "Payload too large", "El archivo supera el tamano maximo permitido."),
            _ => (StatusCodes.Status500InternalServerError, "Internal server error", "Ocurrio un error inesperado.")
        };

        if (statusCode >= 500)
        {
            logger.LogError(exception, "Unhandled exception. CorrelationId={CorrelationId}", correlationId);
        }
        else
        {
            logger.LogWarning(exception, "Handled exception. CorrelationId={CorrelationId}", correlationId);
        }

        var problem = new ProblemDetails
        {
            Title = title,
            Detail = environment.IsDevelopment() ? detail : detail,
            Status = statusCode,
            Type = $"https://httpstatuses.com/{statusCode}",
            Instance = context.Request.Path
        };
        problem.Extensions["correlationId"] = correlationId;

        context.Response.ContentType = "application/problem+json";
        context.Response.StatusCode = statusCode;
        await context.Response.WriteAsJsonAsync(problem);
    }
}
