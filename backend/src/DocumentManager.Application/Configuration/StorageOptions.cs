namespace DocumentManager.Application.Configuration;

public sealed class StorageOptions
{
    public const string SectionName = "Storage";
    public string Provider { get; set; } = "Local";
    public LocalStorageOptions Local { get; set; } = new();
}

public sealed class LocalStorageOptions
{
    public string BasePath { get; set; } = "storage";
}
