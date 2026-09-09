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

        var adminEmail = seedOptions.AdminEmail.Trim();
        var adminUserName = string.IsNullOrWhiteSpace(seedOptions.AdminUserName)
            ? adminEmail
            : seedOptions.AdminUserName.Trim();

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var existing = await userManager.FindByEmailAsync(adminEmail);
        if (existing is not null)
        {
            await SyncExistingAdminAsync(userManager, existing, adminUserName, seedOptions.AdminPassword, seedOptions.ResetAdminPassword);
            return;
        }

        var admin = new ApplicationUser
        {
            UserName = adminUserName,
            Email = adminEmail,
            FirstName = seedOptions.AdminFirstName ?? "Administrador",
            LastName = seedOptions.AdminLastName ?? "Local",
            EmailConfirmed = true,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        var result = await userManager.CreateAsync(admin, seedOptions.AdminPassword);
        EnsureSucceeded(result, "No se pudo crear el administrador de desarrollo. Revise DevelopmentSeed__AdminPassword; debe cumplir la politica de Identity.");

        result = await userManager.AddToRoleAsync(admin, AppRoles.Administrator);
        EnsureSucceeded(result, "No se pudo asignar el rol ADMINISTRATOR al administrador de desarrollo.");
    }

    private static async Task SyncExistingAdminAsync(UserManager<ApplicationUser> userManager, ApplicationUser admin, string adminUserName, string adminPassword, bool resetPassword)
    {
        var changed = false;
        if (!string.Equals(admin.UserName, adminUserName, StringComparison.Ordinal))
        {
            var owner = await userManager.FindByNameAsync(adminUserName);
            if (owner is null || owner.Id == admin.Id)
            {
                admin.UserName = adminUserName;
                changed = true;
            }
        }

        if (!admin.EmailConfirmed)
        {
            admin.EmailConfirmed = true;
            changed = true;
        }

        if (!admin.IsActive)
        {
            admin.IsActive = true;
            changed = true;
        }

        if (changed)
        {
            var updateResult = await userManager.UpdateAsync(admin);
            EnsureSucceeded(updateResult, "No se pudo actualizar el administrador de desarrollo.");
        }

        if (!await userManager.IsInRoleAsync(admin, AppRoles.Administrator))
        {
            var roleResult = await userManager.AddToRoleAsync(admin, AppRoles.Administrator);
            EnsureSucceeded(roleResult, "No se pudo asignar el rol ADMINISTRATOR al administrador de desarrollo existente.");
        }

        if (resetPassword)
        {
            var token = await userManager.GeneratePasswordResetTokenAsync(admin);
            var passwordResult = await userManager.ResetPasswordAsync(admin, token, adminPassword);
            EnsureSucceeded(passwordResult, "No se pudo sincronizar DevelopmentSeed__AdminPassword; debe cumplir la politica de Identity.");
        }
    }

    private static void EnsureSucceeded(IdentityResult result, string message)
    {
        if (result.Succeeded)
        {
            return;
        }

        var errors = string.Join(" ", result.Errors.Select(error => error.Description));
        throw new InvalidOperationException($"{message} {errors}");
    }
}