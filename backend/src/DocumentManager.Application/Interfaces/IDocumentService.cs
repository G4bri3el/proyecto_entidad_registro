using DocumentManager.Application.Common;
using DocumentManager.Application.Documents;

namespace DocumentManager.Application.Interfaces;

public interface IDocumentService
{
    Task<PagedResult<DocumentDto>> ListByFolderAsync(Guid folderId, DocumentQuery query, CancellationToken cancellationToken = default);
    Task<DocumentDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<DocumentDto> UploadAsync(Guid folderId, UploadDocumentCommand command, CancellationToken cancellationToken = default);
    Task<DocumentContentResult> OpenContentAsync(Guid id, bool download, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, DeleteDocumentRequest request, CancellationToken cancellationToken = default);
    Task RestoreAsync(Guid id, CancellationToken cancellationToken = default);
    Task PermanentlyDeleteAsync(Guid id, DeleteDocumentRequest request, CancellationToken cancellationToken = default);
}
