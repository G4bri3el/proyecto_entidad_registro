using DocumentManager.Application.Security;

namespace DocumentManager.Application.Interfaces;

public interface IFileValidationService
{
    Task<ValidatedFile> ValidateAsync(Stream content, string originalFileName, string contentType, long length, CancellationToken cancellationToken = default);
}
