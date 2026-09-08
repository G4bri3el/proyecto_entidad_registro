namespace DocumentManager.Application.Interfaces;

public interface IFileStorage
{
    Task UploadAsync(string storageKey, Stream content, CancellationToken cancellationToken = default);
    Task<Stream> DownloadAsync(string storageKey, CancellationToken cancellationToken = default);
    Task DeleteAsync(string storageKey, CancellationToken cancellationToken = default);
    Task<bool> ExistsAsync(string storageKey, CancellationToken cancellationToken = default);
}
