using System.Security.Cryptography;
using DocumentManager.Application.Security;
using DocumentManager.Domain.Constants;
using DocumentManager.Domain.Exceptions;

namespace DocumentManager.UnitTests;

public sealed class SecurityUtilityTests
{
    [Fact]
    public void Storage_key_is_server_generated_and_safe()
    {
        var key = DocumentStorageKeyFactory.Create(".pdf", new DateTimeOffset(2026, 9, 8, 0, 0, 0, TimeSpan.Zero));

        DocumentStorageKeyFactory.EnsureGeneratedKeyIsSafe(key);

        Assert.StartsWith("documents/2026/09/", key);
        Assert.EndsWith(".pdf", key);
        Assert.DoesNotContain("..", key);
    }

    [Fact]
    public void Storage_key_rejects_path_traversal()
    {
        Assert.Throws<ValidationAppException>(() =>
            DocumentStorageKeyFactory.EnsureGeneratedKeyIsSafe("../../archivo.pdf"));
    }

    [Fact]
    public async Task Sha256_is_computed_from_stream()
    {
        await using var stream = new MemoryStream("hello"u8.ToArray());

        var hash = await Sha256Hasher.ComputeAsync(stream);
        var expected = Convert.ToHexString(SHA256.HashData("hello"u8.ToArray())).ToLowerInvariant();

        Assert.Equal(expected, hash);
        Assert.Equal(0, stream.Position);
    }

    [Fact]
    public void Delete_confirmation_constants_are_exact()
    {
        Assert.Equal("ELIMINAR", DocumentConfirmations.SoftDelete);
        Assert.Equal("ELIMINAR DEFINITIVAMENTE", DocumentConfirmations.PermanentDelete);
    }
}
