namespace DocumentManager.Application.Configuration;

public sealed class DevelopmentSeedOptions
{
    public const string SectionName = "DevelopmentSeed";
    public string? AdminEmail { get; set; }
    public string? AdminPassword { get; set; }
    public string? AdminFirstName { get; set; }
    public string? AdminLastName { get; set; }
}
