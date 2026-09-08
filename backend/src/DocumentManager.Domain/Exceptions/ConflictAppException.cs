namespace DocumentManager.Domain.Exceptions;

public sealed class ConflictAppException(string message) : AppException(message)
{
    public override int StatusCode => 409;
    public override string Title => "Conflict";
}
