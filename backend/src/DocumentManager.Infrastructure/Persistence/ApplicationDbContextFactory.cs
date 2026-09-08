using DocumentManager.Application.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DocumentManager.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var provider = Environment.GetEnvironmentVariable("DOCUMENTMANAGER_DATABASE_PROVIDER")
            ?? DatabaseProviders.PostgreSql;
        var connectionString = Environment.GetEnvironmentVariable("DOCUMENTMANAGER_CONNECTION_STRING")
            ?? "Host=localhost;Port=5432;Database=entidad_registro;Username=postgres;Password=CHANGE_ME";

        if (!DatabaseProviders.IsPostgreSql(provider))
        {
            throw new InvalidOperationException("DOCUMENTMANAGER_DATABASE_PROVIDER debe ser PostgreSQL, Postgres o Npgsql.");
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}