namespace DocumentManager.Application.Configuration;

public sealed class DevelopmentSeedOptions
{
    public const string SectionName = "DevelopmentSeed";
    public string? AdminUserName { get; set; }
    public string? AdminEmail { get; set; }
    public string? AdminPassword { get; set; }
    public string? AdminFirstName { get; set; }
    public string? AdminLastName { get; set; }
    public bool ResetAdminPassword { get; set; }
}