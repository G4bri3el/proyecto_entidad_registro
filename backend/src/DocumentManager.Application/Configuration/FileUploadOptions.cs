namespace DocumentManager.Application.Configuration;

public sealed class FileUploadOptions
{
    public const string SectionName = "FileUpload";
    public int MaxFileSizeMb { get; set; } = 25;
    public string[] AllowedExtensions { get; set; } = [".pdf", ".jpg", ".jpeg", ".png"];
    public string[] AllowedMimeTypes { get; set; } = ["application/pdf", "image/jpeg", "image/png"];
}
