namespace DocumentManager.Application.Documents;

public sealed record UploadDocumentCommand(
    Stream Content,
    string FileName,
    string ContentType,
    long Length);
