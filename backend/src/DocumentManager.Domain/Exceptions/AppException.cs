namespace DocumentManager.Domain.Exceptions;

public abstract class AppException(string message) : Exception(message)
{
    public abstract int StatusCode { get; }
    public virtual string Title => "Application error";
}
