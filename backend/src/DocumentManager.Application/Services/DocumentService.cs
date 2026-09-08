using DocumentManager.Application.Common;
using DocumentManager.Application.Documents;
using DocumentManager.Application.Interfaces;
using DocumentManager.Application.Security;
using DocumentManager.Domain.Constants;
using DocumentManager.Domain.Entities;
using DocumentManager.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace DocumentManager.Application.Services;

public sealed class DocumentService(
    IApplicationDbContext dbContext,
    ICurrentUserService currentUser,
    IFileValidationService fileValidationService,
    IFileScanner fileScanner,
    IFileStorage fileStorage,
    IAuditService auditService) : IDocumentService
{
    public async Task<PagedResult<DocumentDto>> ListByFolderAsync(Guid folderId, DocumentQuery query, CancellationToken cancellationToken = default)
    {
        await EnsureFolderExistsAsync(folderId, cancellationToken);

        var page = query.SafePage;
        var pageSize = query.SafePageSize;

        var documentsQuery = dbContext.Documents
            .AsNoTracking()
            .Where(document => document.FolderId == folderId && !document.IsDeleted)
            .OrderByDescending(document => document.UploadedAt);

        var total = await documentsQuery.CountAsync(cancellationToken);
        var documents = await documentsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(document => ToDto(document))
            .ToListAsync(cancellationToken);

        return PagedResult<DocumentDto>.Create(documents, page, pageSize, total);
    }

    public async Task<DocumentDto> GetAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await GetActiveDocumentAsync(id, cancellationToken);
        return ToDto(document);
    }

    public async Task<DocumentDto> UploadAsync(Guid folderId, UploadDocumentCommand command, CancellationToken cancellationToken = default)
    {
        var userId = RequireUser();
        await EnsureFolderExistsAsync(folderId, cancellationToken);

        var validatedFile = await fileValidationService.ValidateAsync(
            command.Content,
            command.FileName,
            command.ContentType,
            command.Length,
            cancellationToken);

        var scanResult = await fileScanner.ScanAsync(command.Content, validatedFile.SanitizedFileName, cancellationToken);
        if (!scanResult.IsClean)
        {
            throw new ValidationAppException("El archivo no supero el analisis de seguridad.", 415);
        }

        var sha256 = await Sha256Hasher.ComputeAsync(command.Content, cancellationToken);
        var now = DateTimeOffset.UtcNow;
        var storageKey = DocumentStorageKeyFactory.Create(validatedFile.Extension, now);
        DocumentStorageKeyFactory.EnsureGeneratedKeyIsSafe(storageKey);
        var storedFileName = Path.GetFileName(storageKey);

        await fileStorage.UploadAsync(storageKey, command.Content, cancellationToken);

        var document = new Document
        {
            Id = Guid.NewGuid(),
            FolderId = folderId,
            OriginalFileName = validatedFile.SanitizedFileName,
            StoredFileName = storedFileName,
            StorageKey = storageKey,
            MimeType = validatedFile.MimeType,
            Extension = validatedFile.Extension,
            Size = validatedFile.Size,
            Sha256 = sha256,
            UploadedByUserId = userId,
            UploadedAt = now
        };

        try
        {
            dbContext.Documents.Add(document);
            await auditService.RecordAsync(
                AuditActions.FileUploaded,
                nameof(Document),
                document.Id.ToString(),
                $"Archivo cargado: {document.OriginalFileName}",
                new { document.FolderId, document.Size, document.Sha256 },
                cancellationToken);
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            await fileStorage.DeleteAsync(storageKey, cancellationToken);
            throw;
        }

        return ToDto(document);
    }

    public async Task<DocumentContentResult> OpenContentAsync(Guid id, bool download, CancellationToken cancellationToken = default)
    {
        var document = await GetActiveDocumentAsync(id, cancellationToken);
        if (!await fileStorage.ExistsAsync(document.StorageKey, cancellationToken))
        {
            throw new NotFoundAppException("El archivo fisico no existe en el almacenamiento configurado.");
        }

        var stream = await fileStorage.DownloadAsync(document.StorageKey, cancellationToken);
        await auditService.RecordAsync(
            download ? AuditActions.FileDownloaded : AuditActions.FileViewed,
            nameof(Document),
            document.Id.ToString(),
            download ? $"Archivo descargado: {document.OriginalFileName}" : $"Archivo visualizado: {document.OriginalFileName}",
            new { document.FolderId },
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);

        return new DocumentContentResult(stream, document.MimeType, document.OriginalFileName, document.Size);
    }

    public async Task DeleteAsync(Guid id, DeleteDocumentRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(request.Confirmation, DocumentConfirmations.SoftDelete, StringComparison.Ordinal))
        {
            throw new ValidationAppException("Debe escribir ELIMINAR para confirmar la eliminacion.");
        }

        var document = await GetActiveDocumentAsync(id, cancellationToken);
        document.IsDeleted = true;
        document.DeletedAt = DateTimeOffset.UtcNow;
        document.DeletedByUserId = RequireUser();
        document.UpdatedAt = DateTimeOffset.UtcNow;

        await auditService.RecordAsync(
            AuditActions.FileDeleted,
            nameof(Document),
            document.Id.ToString(),
            $"Archivo enviado a papelera: {document.OriginalFileName}",
            new { document.FolderId },
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var document = await dbContext.Documents.FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("El documento eliminado no existe.");

        document.IsDeleted = false;
        document.DeletedAt = null;
        document.DeletedByUserId = null;
        document.UpdatedAt = DateTimeOffset.UtcNow;

        await auditService.RecordAsync(
            AuditActions.FileRestored,
            nameof(Document),
            document.Id.ToString(),
            $"Archivo restaurado: {document.OriginalFileName}",
            new { document.FolderId },
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task PermanentlyDeleteAsync(Guid id, DeleteDocumentRequest request, CancellationToken cancellationToken = default)
    {
        if (!string.Equals(request.Confirmation, DocumentConfirmations.PermanentDelete, StringComparison.Ordinal))
        {
            throw new ValidationAppException("Debe escribir ELIMINAR DEFINITIVAMENTE para confirmar la eliminacion definitiva.");
        }

        var document = await dbContext.Documents.FirstOrDefaultAsync(item => item.Id == id && item.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("El documento eliminado no existe.");

        await fileStorage.DeleteAsync(document.StorageKey, cancellationToken);

        await auditService.RecordAsync(
            AuditActions.FilePermanentlyDeleted,
            nameof(Document),
            document.Id.ToString(),
            $"Archivo eliminado definitivamente: {document.OriginalFileName}",
            new { document.FolderId, document.StorageKey },
            cancellationToken);
        dbContext.Documents.Remove(document);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task EnsureFolderExistsAsync(Guid folderId, CancellationToken cancellationToken)
    {
        var exists = await dbContext.Folders.AnyAsync(folder => folder.Id == folderId && !folder.IsDeleted, cancellationToken);
        if (!exists)
        {
            throw new NotFoundAppException("La carpeta no existe.");
        }
    }

    private async Task<Document> GetActiveDocumentAsync(Guid id, CancellationToken cancellationToken)
    {
        return await dbContext.Documents.FirstOrDefaultAsync(document => document.Id == id && !document.IsDeleted, cancellationToken)
            ?? throw new NotFoundAppException("El documento no existe.");
    }

    private string RequireUser() => currentUser.UserId ?? throw new ForbiddenAppException();

    private static DocumentDto ToDto(Document document) =>
        new(
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
            document.DeletedAt);
}
