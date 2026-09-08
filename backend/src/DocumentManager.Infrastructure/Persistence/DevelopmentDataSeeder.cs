using DocumentManager.Application.Configuration;
using DocumentManager.Domain.Constants;
using DocumentManager.Domain.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace DocumentManager.Infrastructure.Persistence;

public static class DevelopmentDataSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var environment = scope.ServiceProvider.GetRequiredService<IHostEnvironment>();
        if (!environment.IsDevelopment())
        {
            return;
        }

        var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        if (dbContext.Database.IsRelational())
        {
            if (!await dbContext.Database.CanConnectAsync())
            {
                return;
            }

            var pendingMigrations = await dbContext.Database.GetPendingMigrationsAsync();
            if (pendingMigrations.Any())
            {
                return;
            }
        }

        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var role in AppRoles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        var seedOptions = scope.ServiceProvider.GetRequiredService<IOptions<DevelopmentSeedOptions>>().Value;
        if (string.IsNullOrWhiteSpace(seedOptions.AdminEmail) || string.IsNullOrWhiteSpace(seedOptions.AdminPassword))
        {
            return;
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var existing = await userManager.FindByEmailAsync(seedOptions.AdminEmail);
        if (existing is not null)
        {
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = seedOptions.AdminEmail,
            Email = seedOptions.AdminEmail,
            FirstName = seedOptions.AdminFirstName ?? "Administrador",
            LastName = seedOptions.AdminLastName ?? "Local",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(admin, seedOptions.AdminPassword);
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, AppRoles.Administrator);
        }
    }
}
