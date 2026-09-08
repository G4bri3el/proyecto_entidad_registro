namespace DocumentManager.Application.Folders;

public sealed record FolderDto(
    Guid Id,
    string Name,
    Guid? ParentFolderId,
    DateTimeOffset CreatedAt,
    bool IsDeleted,
    IReadOnlyCollection<FolderDto> Children);

public sealed record CreateFolderRequest(string Name, Guid? ParentFolderId);

public sealed record UpdateFolderRequest(string Name);

public sealed record MoveFolderRequest(Guid? ParentFolderId);
