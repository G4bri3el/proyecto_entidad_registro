namespace DocumentManager.Domain.Exceptions;

public sealed class UnauthorizedAppException(string message = "La sesion no es valida o ha expirado.") : AppException(message)
{
    public override int StatusCode => 401;
    public override string Title => "Unauthorized";
}
