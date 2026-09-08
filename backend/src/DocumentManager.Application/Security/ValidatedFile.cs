namespace DocumentManager.Application.Security;

public sealed record ValidatedFile(
    string OriginalFileName,
    string SanitizedFileName,
    string Extension,
    string MimeType,
    long Size);
