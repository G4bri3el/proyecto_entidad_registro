namespace DocumentManager.Domain.ValueObjects;

public sealed record AllowedFileSignature(string Extension, string MimeType, byte[][] MagicBytes);
