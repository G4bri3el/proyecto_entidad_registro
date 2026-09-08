using DocumentManager.Application.Folders;
using DocumentManager.Application.Interfaces;
using DocumentManager.Domain.Constants;
using DocumentManager.Domain.Entities;
using DocumentManager.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace DocumentManager.Application.Services;

public sealed class FolderService(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser,
    IAuditService auditService) : IFolderService
{
    public async Task<IReadOnlyCollection<FolderDto>> GetTreeAsync(CancellationToken cancellationToken = default)
    {
        var folders = await dbContext.Folders
            .AsNoTracking()
            .Where(folder => !folder.IsDeleted)
            .OrderBy(folder => folder.Name)
            .ToListAsync(cancellationToken);

        var lookup = folders.ToLookup(folder => folder.ParentFolderId);
        FolderDto Map(Folder folder) => new(
            folder.Id,
            folder.Name,
            folder.ParentFolderId,
            folder.CreatedAt,
            folder.IsDeleted,
            lookup[folder.Id].Select(Map).ToList());

        return lookup[null].Select(Map).ToList();
    }

    public async Task<FolderDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var folder = await GetActiveFolderAsync(id, cancellationToken);
        return ToDto(folder);
    }

    public async Task<FolderDto> CreateAsync(CreateFolderRequest request, CancellationToken cancellationToken = default)
    {
        var name = ValidateName(request.Name);
        if (request.ParentFolderId is not null)
        {
            await EnsureFolderExistsAsync(request.ParentFolderId.Value, cancellationToken);
        }

        await EnsureSiblingNameIsUniqueAsync(name, request.ParentFolderId, null, cancellationToken);

        var folder = new Folder
        {
            Id = Guid.NewGuid(),
            Name = name,
            ParentFolderId = request.ParentFolderId,
            CreatedByUserId = RequireUser(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Folders.Add(folder);
        await auditService.RecordAsync(
            AuditActions.FolderCreated,
            nameof(Folder),
            folder.Id.ToString(),
            $"Carpeta creada: {folder.Name}",
            new { folder.ParentFolderId },
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(folder);
    }

    public async Task<FolderDto> RenameAsync(Guid id, UpdateFolderRequest request, CancellationToken cancellationToken = default)
    {
        var folder = await GetActiveFolderAsync(id, cancellationToken);
        var name = ValidateName(request.Name);
        await EnsureSiblingNameIsUniqueAsync(name, folder.ParentFolderId, folder.Id, cancellationToken);

        folder.Name = name;
        folder.UpdatedAt = DateTimeOffset.UtcNow;
        await auditService.RecordAsync(
            AuditActions.FolderRenamed,
            nameof(Folder),
            folder.Id.ToString(),
            $"Carpeta renombrada: {folder.Name}",
            null,
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(folder);
    }

    public async Task<FolderDto> MoveAsync(Guid id, MoveFolderRequest request, CancellationToken cancellationToken = default)
    {
        var folder = await GetActiveFolderAsync(id, cancellationToken);
        if (request.ParentFolderId == folder.Id)
        {
            throw new ValidationAppException("No se puede mover una carpeta dentro de si misma.");
        }

        if (request.ParentFolderId is not null)
        {
            await EnsureFolderExistsAsync(request.ParentFolderId.Value, cancellationToken);
            if (await IsDescendantAsync(folder.Id, request.ParentFolderId.Value, cancellationToken))
            {
                throw new ValidationAppException("No se puede mover una carpeta dentro de uno de sus descendientes.");
            }
        }

        await EnsureSiblingNameIsUniqueAsync(folder.Name, request.ParentFolderId, folder.Id, cancellationToken);
        folder.ParentFolderId = request.ParentFolderId;
        folder.UpdatedAt = DateTimeOffset.UtcNow;

        await auditService.RecordAsync(
            AuditActions.FolderMoved,
            nameof(Folder),
            folder.Id.ToString(),
            $"Carpeta movida: {folder.Name}",
            new { folder.ParentFolderId },
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
        return ToDto(folder);
    }

    public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var userId = RequireUser();
        var folders = await dbContext.Folders.Where(folder => !folder.IsDeleted).ToListAsync(cancellationToken);
        var root = folders.FirstOrDefault(folder => folder.Id == id)
            ?? throw new NotFoundAppException("La carpeta no existe.");

        var ids = CollectSubtreeIds(folders, root.Id);
        var now = DateTimeOffset.UtcNow;

        foreach (var folder in folders.Where(folder => ids.Contains(folder.Id)))
        {
            folder.IsDeleted = true;
            folder.DeletedAt = now;
            folder.DeletedByUserId = userId;
            folder.UpdatedAt = now;
        }

        var documents = await dbContext.Documents
            .Where(document => ids.Contains(document.FolderId) && !document.IsDeleted)
            .ToListAsync(cancellationToken);

        foreach (var document in documents)
        {
            document.IsDeleted = true;
            document.DeletedAt = now;
            document.DeletedByUserId = userId;
            document.UpdatedAt = now;
        }

        await auditService.RecordAsync(
            AuditActions.FolderDeleted,
            nameof(Folder),
            root.Id.ToString(),
            $"Carpeta enviada a papelera: {root.Name}",
            new { DocumentsAffected = documents.Count, FoldersAffected = ids.Count },
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static HashSet<Guid> CollectSubtreeIds(IReadOnlyCollection<Folder> folders, Guid rootId)
    {
        var children = folders.ToLookup(folder => folder.ParentFolderId);
        var result = new HashSet<Guid>();
        var stack = new Stack<Guid>();
        stack.Push(rootId);

        while (stack.TryPop(out var current))
        {
            if (!result.Add(current))
            {
                continue;
            }

            foreach (var child in children[current])
            {
                stack.Push(child.Id);
            }
        }

        return result;
    }

    private async Task<bool> IsDescendantAsync(Guid folderId, Guid candidateParentId, CancellationToken cancellationToken)
    {
        var current = await dbContext.Folders
            .AsNoTracking()
            .Where(folder => folder.Id == candidateParentId)
            .Select(folder => new { folder.Id, folder.ParentFolderId })
            .FirstOrDefaultAsync(cancellationToken);

        while (current is not null)
        {
            if (current.Id == folderId)
            {
                return true;
            }

            if (current.ParentFolderId is null)
            {
                return false;
            }

            current = await dbContext.Folders
                .AsNoTracking()
                .Where(folder => folder.Id == current.ParentFolderId.Value)
                .Select(folder => new { folder.Id, folder.ParentFolderId })
                .FirstOrDefaultAsync(cancellationToken);
        }

        return false;
    }

    private async Task EnsureFolderExistsAsync(Guid id, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Folders.AnyAsync(folder => folder.Id == id && !folder.IsDeleted, cancellationToken);
        if (!exists)
        {
            throw new NotFoundAppException("La carpeta padre no existe.");
        }
    }

    private async Task<Folder> GetActiveFolderAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Folders.FirstOrDefaultAsync(folder => folder.Id == id && !folder.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("La carpeta no existe.");
    }

    private async Task EnsureSiblingNameIsUniqueAsync(string name, Guid? parentId, Guid? excludingId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Folders.AnyAsync(folder =>
            !folder.IsDeleted &&
            folder.ParentFolderId == parentId &&
            folder.Name == name &&
            (excludingId == null || folder.Id != excludingId.Value),
            cancellationToken);

        if (exists)
        {
            throw new ConflictAppException("Ya existe una carpeta con ese nombre en la misma ubicacion.");
        }
    }

    private static string ValidateName(string name)
    {
        var trimmed = name.Trim();
        if (trimmed.Length is < 1 or > 150)
        {
            throw new ValidationAppException("El nombre de la carpeta debe tener entre 1 y 150 caracteres.");
        }

        if (trimmed.Contains("..", StringComparison.Ordinal) ||
            trimmed.Contains('/', StringComparison.Ordinal) ||
            trimmed.Contains('\\', StringComparison.Ordinal) ||
            trimmed.Any(char.IsControl))
        {
            throw new ValidationAppException("El nombre de la carpeta contiene caracteres invalidos.");
        }

        return trimmed;
    }

    private string RequireUser() => currentUser.UserId ?? throw new ForbiddenAppException();

    private static FolderDto ToDto(Folder folder) =>
        new(folder.Id, folder.Name, folder.ParentFolderId, folder.CreatedAt, folder.IsDeleted, []);
}
