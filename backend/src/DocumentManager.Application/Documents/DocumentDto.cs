namespace DocumentManager.Application.Documents;

public sealed record DocumentDto(
    Guid Id,
    Guid FolderId,
    string OriginalFileName,
    string MimeType,
    string Extension,
    long Size,
    string Sha256,
    string UploadedByUserId,
    DateTimeOffset UploadedAt,
    bool IsDeleted,
    DateTimeOffset? DeletedAt);
