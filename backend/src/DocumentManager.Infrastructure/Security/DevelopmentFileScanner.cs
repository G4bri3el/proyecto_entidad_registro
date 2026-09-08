using DocumentManager.Application.Interfaces;

namespace DocumentManager.Infrastructure.Security;

public sealed class DevelopmentFileScanner : IFileScanner
{
    public Task<FileScanResult> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken = default)
    {
        if (content.CanSeek)
        {
            content.Position = 0;
        }

        return Task.FromResult(FileScanResult.Clean());
    }
}
