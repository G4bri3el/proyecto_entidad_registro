using DocumentManager.Application.Common;
using DocumentManager.Application.Documents;
using DocumentManager.Application.Folders;

namespace DocumentManager.Application.Interfaces;

public interface ITrashService
{
    Task<PagedResult<DocumentDto>> GetDeletedDocumentsAsync(PageRequest request, CancellationToken cancellationToken = default);
    Task<PagedResult<FolderDto>> GetDeletedFoldersAsync(PageRequest request, CancellationToken cancellationToken = default);
}
