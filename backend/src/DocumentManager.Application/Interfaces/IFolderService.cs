using DocumentManager.Application.Folders;

namespace DocumentManager.Application.Interfaces;

public interface IFolderService
{
    Task<IReadOnlyCollection<FolderDto>> GetTreeAsync(CancellationToken cancellationToken = default);
    Task<FolderDto> GetAsync(Guid id, CancellationToken cancellationToken = default);
    Task<FolderDto> CreateAsync(CreateFolderRequest request, CancellationToken cancellationToken = default);
    Task<FolderDto> RenameAsync(Guid id, UpdateFolderRequest request, CancellationToken cancellationToken = default);
    Task<FolderDto> MoveAsync(Guid id, MoveFolderRequest request, CancellationToken cancellationToken = default);
    Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
}
