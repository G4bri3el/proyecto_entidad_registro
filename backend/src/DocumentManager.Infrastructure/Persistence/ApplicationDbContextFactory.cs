using DocumentManager.Application.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace DocumentManager.Infrastructure.Persistence;

public sealed class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        DotEnvLoader.LoadNearest(Directory.GetCurrentDirectory(), AppContext.BaseDirectory);

        var provider = Environment.GetEnvironmentVariable("DOCUMENTMANAGER_DATABASE_PROVIDER")
            ?? Environment.GetEnvironmentVariable("Database__Provider")
            ?? DatabaseProviders.PostgreSql;
        var connectionString = Environment.GetEnvironmentVariable("DOCUMENTMANAGER_CONNECTION_STRING")
            ?? Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Host=localhost;Port=5432;Database=entidad_registro;Username=postgres;Password=CHANGE_ME";

        if (!DatabaseProviders.IsPostgreSql(provider))
        {
            throw new InvalidOperationException("DOCUMENTMANAGER_DATABASE_PROVIDER o Database__Provider debe ser PostgreSQL, Postgres o Npgsql.");
        }

        var options = new DbContextOptionsBuilder<ApplicationDbContext>()
            .UseNpgsql(connectionString)
            .Options;

        return new ApplicationDbContext(options);
    }
}