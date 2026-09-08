namespace DocumentManager.Application.Configuration;

public sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";
    public int AuthPermitLimit { get; set; } = 8;
    public int AuthWindowSeconds { get; set; } = 60;
    public int RefreshPermitLimit { get; set; } = 20;
    public int RefreshWindowSeconds { get; set; } = 60;
}
