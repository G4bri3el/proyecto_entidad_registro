namespace DocumentManager.Domain.Exceptions;

public sealed class ValidationAppException(string message, int statusCode = 400) : AppException(message)
{
    public override int StatusCode { get; } = statusCode;
    public override string Title => "Validation error";
}
