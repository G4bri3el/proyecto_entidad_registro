namespace DocumentManager.Domain.Exceptions;

public sealed class NotFoundAppException(string message = "El recurso solicitado no existe.") : AppException(message)
{
    public override int StatusCode => 404;
    public override string Title => "Not found";
}
