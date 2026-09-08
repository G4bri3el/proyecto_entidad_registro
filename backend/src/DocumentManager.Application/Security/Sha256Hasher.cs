using System.Security.Cryptography;

namespace DocumentManager.Application.Security;

public static class Sha256Hasher
{
    public static async Task<string> ComputeAsync(Stream content, CancellationToken cancellationToken = default)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        using var sha256 = SHA256.Create();
        var hash = await sha256.ComputeHashAsync(content, cancellationToken);

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        return Convert.ToHexString(hash).ToLowerInvariant();
    }
}
