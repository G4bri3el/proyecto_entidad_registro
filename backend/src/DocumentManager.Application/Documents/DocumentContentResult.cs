namespace DocumentManager.Application.Documents;

public sealed record DocumentContentResult(
    Stream Content,
    string ContentType,
    string FileName,
    long Size);
