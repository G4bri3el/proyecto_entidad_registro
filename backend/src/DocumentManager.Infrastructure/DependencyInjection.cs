using System.Text;
using DocumentManager.Application.Configuration;
using DocumentManager.Application.Interfaces;
using DocumentManager.Domain.Constants;
using DocumentManager.Domain.Entities;
using DocumentManager.Infrastructure.Audit;
using DocumentManager.Infrastructure.Authentication;
using DocumentManager.Infrastructure.Persistence;
using DocumentManager.Infrastructure.Security;
using DocumentManager.Infrastructure.Storage;
using DocumentManager.Infrastructure.Users;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace DocumentManager.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructureServices(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtOptions>(configuration.GetSection(JwtOptions.SectionName));
        services.Configure<FileUploadOptions>(configuration.GetSection(FileUploadOptions.SectionName));
        services.Configure<StorageOptions>(configuration.GetSection(StorageOptions.SectionName));
        services.Configure<CorsOptions>(configuration.GetSection(CorsOptions.SectionName));
        services.Configure<RateLimitingOptions>(configuration.GetSection(RateLimitingOptions.SectionName));
        services.Configure<DevelopmentSeedOptions>(configuration.GetSection(DevelopmentSeedOptions.SectionName));
        services.Configure<DatabaseOptions>(configuration.GetSection(DatabaseOptions.SectionName));

        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection no esta configurado.");

        var databaseOptions = configuration.GetSection(DatabaseOptions.SectionName).Get<DatabaseOptions>() ?? new DatabaseOptions();
        if (!DatabaseProviders.IsPostgreSql(databaseOptions.Provider))
        {
            throw new InvalidOperationException("Database:Provider debe ser PostgreSQL, Postgres o Npgsql.");
        }

        services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(connectionString));
        services.AddScoped<IApplicationDbContext>(provider => provider.GetRequiredService<ApplicationDbContext>());

        services
            .AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequiredLength = 12;
                options.Password.RequireUppercase = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireDigit = true;
                options.Password.RequireNonAlphanumeric = false;
                options.Lockout.AllowedForNewUsers = true;
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(10);
                options.User.RequireUniqueEmail = true;
            })
            .AddEntityFrameworkStores<ApplicationDbContext>()
            .AddDefaultTokenProviders();

        services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var jwtOptions = configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidateAudience = true,
                    ValidAudience = jwtOptions.Audience,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = CreateSigningKey(jwtOptions),
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30)
                };
            });

        services.AddAuthorization(RegisterPolicies);
        services.AddHttpContextAccessor();
        services.AddScoped<ITokenService, TokenService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICurrentUserService, CurrentUserService>();
        services.AddScoped<IAuditService, AuditService>();
        services.AddScoped<IFileScanner, DevelopmentFileScanner>();
        services.AddScoped<IFileStorage, LocalFileStorage>();

        return services;
    }

    private static SymmetricSecurityKey CreateSigningKey(JwtOptions jwtOptions)
    {
        if (string.IsNullOrWhiteSpace(jwtOptions.SigningKey) || jwtOptions.SigningKey.Length < 32)
        {
            throw new InvalidOperationException("Jwt:SigningKey debe configurarse por user-secrets o variables de entorno y tener al menos 32 caracteres.");
        }

        return new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));
    }

    private static void RegisterPolicies(AuthorizationOptions options)
    {
        options.AddPolicy(AppPolicies.CanManageUsers, policy => policy.RequireRole(AppRoles.Administrator));
        options.AddPolicy(AppPolicies.CanManageRoles, policy => policy.RequireRole(AppRoles.Administrator));
        options.AddPolicy(AppPolicies.CanCreateFolders, policy => policy.RequireRole(AppRoles.Administrator, AppRoles.Editor));
        options.AddPolicy(AppPolicies.CanRenameFolders, policy => policy.RequireRole(AppRoles.Administrator));
        options.AddPolicy(AppPolicies.CanMoveFolders, policy => policy.RequireRole(AppRoles.Administrator));
        options.AddPolicy(AppPolicies.CanDeleteFolders, policy => policy.RequireRole(AppRoles.Administrator));
        options.AddPolicy(AppPolicies.CanUploadDocuments, policy => policy.RequireRole(AppRoles.Administrator, AppRoles.Editor));
        options.AddPolicy(AppPolicies.CanViewDocuments, policy => policy.RequireRole(AppRoles.Administrator, AppRoles.Editor, AppRoles.Viewer));
        options.AddPolicy(AppPolicies.CanDownloadDocuments, policy => policy.RequireRole(AppRoles.Administrator, AppRoles.Editor, AppRoles.Viewer));
        options.AddPolicy(AppPolicies.CanDeleteDocuments, policy => policy.RequireRole(AppRoles.Administrator, AppRoles.Editor));
        options.AddPolicy(AppPolicies.CanRestoreDocuments, policy => policy.RequireRole(AppRoles.Administrator));
        options.AddPolicy(AppPolicies.CanPermanentlyDeleteDocuments, policy => policy.RequireRole(AppRoles.Administrator));
        options.AddPolicy(AppPolicies.CanViewAudit, policy => policy.RequireRole(AppRoles.Administrator));
    }
}
