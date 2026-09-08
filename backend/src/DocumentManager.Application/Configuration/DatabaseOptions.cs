namespace DocumentManager.Application.Configuration;

public sealed class DatabaseOptions
{
    public const string SectionName = "Database";
    public string Provider { get; set; } = DatabaseProviders.PostgreSql;
}

public static class DatabaseProviders
{
    public const string PostgreSql = "PostgreSQL";

    public static bool IsPostgreSql(string? provider) =>
        string.Equals(provider, PostgreSql, StringComparison.OrdinalIgnoreCase) ||
        string.Equals(provider, "Postgres", StringComparison.OrdinalIgnoreCase) ||
        string.Equals(provider, "Npgsql", StringComparison.OrdinalIgnoreCase);
}