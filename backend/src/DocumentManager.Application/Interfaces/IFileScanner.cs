namespace DocumentManager.Application.Interfaces;

public interface IFileScanner
{
    Task<FileScanResult> ScanAsync(Stream content, string fileName, CancellationToken cancellationToken = default);
}

public sealed record FileScanResult(bool IsClean, string? ThreatName = null)
{
    public static FileScanResult Clean() => new(true);
    public static FileScanResult Threat(string threatName) => new(false, threatName);
}
