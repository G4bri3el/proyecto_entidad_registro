using DocumentManager.Application.Common;
using DocumentManager.Application.Documents;
using DocumentManager.Application.Folders;
using DocumentManager.Application.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace DocumentManager.Application.Services;

public sealed class TrashService(IApplicationDbContext dbContext) : ITrashService
{
    public async Task<PagedResult<DocumentDto>> GetDeletedDocumentsAsync(PageRequest request, CancellationToken cancellationToken = default)
    {
        var page = request.SafePage;
        var pageSize = request.SafePageSize;
        var query = dbContext.Documents.AsNoTracking().Where(document => document.IsDeleted).OrderByDescending(document => document.DeletedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(document => new DocumentDto(
                document.Id,
                document.FolderId,
                document.OriginalFileName,
                document.MimeType,
                document.Extension,
                document.Size,
                document.Sha256,
                document.UploadedByUserId,
                document.UploadedAt,
                document.IsDeleted,
                document.DeletedAt))
            .ToListAsync(cancellationToken);

        return PagedResult<DocumentDto>.Create(items, page, pageSize, total);
    }

    public async Task<PagedResult<FolderDto>> GetDeletedFoldersAsync(PageRequest request, CancellationToken cancellationToken = default)
    {
        var page = request.SafePage;
        var pageSize = request.SafePageSize;
        var query = dbContext.Folders.AsNoTracking().Where(folder => folder.IsDeleted).OrderByDescending(folder => folder.DeletedAt);
        var total = await query.CountAsync(cancellationToken);
        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(folder => new FolderDto(folder.Id, folder.Name, folder.ParentFolderId, folder.CreatedAt, folder.IsDeleted, Array.Empty<FolderDto>()))
            .ToListAsync(cancellationToken);

        return PagedResult<FolderDto>.Create(items, page, pageSize, total);
    }
}
