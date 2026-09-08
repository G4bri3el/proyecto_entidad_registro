using DocumentManager.Application.Configuration;
using DocumentManager.Application.Interfaces;
using DocumentManager.Application.Security;
using DocumentManager.Domain.Exceptions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DocumentManager.Infrastructure.Storage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _basePath;

    public LocalFileStorage(IOptions<StorageOptions> optionsAccessor, IHostEnvironment environment)
    {
        var configuredPath = optionsAccessor.Value.Local.BasePath;
        _basePath = Path.GetFullPath(Path.IsPathRooted(configuredPath)
            ? configuredPath
            : Path.Combine(environment.ContentRootPath, configuredPath));

        Directory.CreateDirectory(_basePath);
    }

    public async Task UploadAsync(string storageKey, Stream content, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        if (content.CanSeek)
        {
            content.Position = 0;
        }

        await using var output = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true);
        await content.CopyToAsync(output, cancellationToken);

        if (content.CanSeek)
        {
            content.Position = 0;
        }
    }

    public Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (!File.Exists(path))
        {
            throw new NotFoundAppException("El archivo no existe en almacenamiento.");
        }

        Stream stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, useAsync: true);
        return Task.FromResult(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        if (File.Exists(path))
        {
            File.Delete(path);
        }

        return Task.CompletedTask;
    }

    public Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default)
    {
        var path = ResolvePath(storageKey);
        return Task.FromResult(File.Exists(path));
    }

    private string ResolvePath(string storageKey)
    {
        DocumentStorageKeyFactory.EnsureGeneratedKeyIsSafe(storageKey);
        var relativePath = storageKey.Replace('/', Path.DirectorySeparatorChar);
        var fullPath = Path.GetFullPath(Path.Combine(_basePath, relativePath));
        var baseWithSeparator = _basePath.EndsWith(Path.DirectorySeparatorChar)
            ? _basePath
            : _basePath + Path.DirectorySeparatorChar;

        if (!fullPath.StartsWith(baseWithSeparator, StringComparison.OrdinalIgnoreCase))
        {
            throw new ValidationAppException("La ruta de almacenamiento es invalida.");
        }

        return fullPath;
    }
}
