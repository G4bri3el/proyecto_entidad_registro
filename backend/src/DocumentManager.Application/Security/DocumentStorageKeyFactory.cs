using System.Text.RegularExpressions;
using DocumentManager.Domain.Exceptions;

namespace DocumentManager.Application.Security;

public static partial class DocumentStorageKeyFactory
{
    public static string Create(string extension, DateTimeOffset now)
    {
        if (!extension.StartsWith(".", StringComparison.Ordinal))
        {
            throw new ValidationAppException("La extension del archivo es invalida.");
        }

        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        return $"documents/{now:yyyy}/{now:MM}/{storedName}";
    }

    public static void EnsureGeneratedKeyIsSafe(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) ||
            storageKey.Contains("..", StringComparison.Ordinal) ||
            storageKey.Contains('\\', StringComparison.Ordinal) ||
            !SafeStorageKeyRegex().IsMatch(storageKey))
        {
            throw new ValidationAppException("StorageKey invalida.");
        }
    }

    [GeneratedRegex("^documents/[0-9]{4}/[0-9]{2}/[a-f0-9]{32}\\.(pdf|jpg|jpeg|png)$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeStorageKeyRegex();
}
