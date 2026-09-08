namespace DocumentManager.Domain.Exceptions;

public sealed class ForbiddenAppException(string message = "No tiene permisos para realizar esta accion.") : AppException(message)
{
    public override int StatusCode => 403;
    public override string Title => "Forbidden";
}
