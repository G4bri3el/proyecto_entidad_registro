using DocumentManager.Application.Configuration;
using DocumentManager.Application.Interfaces;
using DocumentManager.Application.Security;
using DocumentManager.Domain.Exceptions;
using Microsoft.Extensions.Options;

namespace DocumentManager.Application.Services;

public sealed class FileValidationService(IOptions<FileUploadOptions> optionsAccessor) : IFileValidationService
{
    private static readonly Dictionary<string, byte[][]> Signatures = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = [[0x25, 0x50, 0x44, 0x46, 0x2D]],
        [".jpg"] = [[0xFF, 0xD8, 0xFF]],
        [".jpeg"] = [[0xFF, 0xD8, 0xFF]],
        [".png"] = [[0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A]]
    };

    private static readonly Dictionary<string, string> ExpectedMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".pdf"] = "application/pdf",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".png"] = "image/png"
    };

    public async Task<ValidatedFile> ValidateAsync(Stream content, string originalFileName, string contentType, long length, CancellationToken cancellationToken = default)
    {
        var options = optionsAccessor.Value;
        if (length <= 0)
        {
            throw new ValidationAppException("El archivo esta vacio.");
        }

        var maxBytes = options.MaxFileSizeMb * 1024L * 1024L;
        if (length > maxBytes)
        {
            throw new ValidationAppException("El archivo supera el tamano maximo permitido.", 413);
        }

        var sanitizedName = FileNameSanitizer.Sanitize(originalFileName);
        var extension = Path.GetExtension(sanitizedName).ToLowerInvariant();
        if (!options.AllowedExtensions.Contains(extension, StringComparer.OrdinalIgnoreCase) || !Signatures.ContainsKey(extension))
        {
            throw new ValidationAppException("La extension del archivo no esta permitida.", 415);
        }

        var normalizedContentType = NormalizeContentType(contentType);
        if (!options.AllowedMimeTypes.Contains(normalizedContentType, StringComparer.OrdinalIgnoreCase))
        {
            throw new ValidationAppException("El tipo de contenido no esta permitido.", 415);
        }

        if (!string.Equals(ExpectedMimeTypes[extension], normalizedContentType, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationAppException("La extension y el tipo de contenido no coinciden.", 415);
        }

        await ValidateMagicBytesAsync(content, extension, cancellationToken);
        return new ValidatedFile(originalFileName, sanitizedName, extension, normalizedContentType, length);
    }

    private static async Task ValidateMagicBytesAsync(Stream content, string extension, CancellationToken cancellationToken)
    {
        if (!content.CanSeek)
        {
            throw new ValidationAppException("El stream del archivo debe permitir lectura segura.");
        }

        content.Position = 0;
        var maxSignatureLength = Signatures[extension].Max(signature => signature.Length);
        var buffer = new byte[maxSignatureLength];
        var read = await content.ReadAsync(buffer.AsMemory(0, maxSignatureLength), cancellationToken);
        content.Position = 0;

        var matches = Signatures[extension].Any(signature =>
            read >= signature.Length && buffer.AsSpan(0, signature.Length).SequenceEqual(signature));

        if (!matches)
        {
            throw new ValidationAppException("La firma del archivo no corresponde al tipo permitido.", 415);
        }
    }

    private static string NormalizeContentType(string contentType)
    {
        var value = contentType.Split(';', 2)[0].Trim().ToLowerInvariant();
        return string.IsNullOrWhiteSpace(value) ? "application/octet-stream" : value;
    }
}
