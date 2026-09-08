using System.Net.Http.Headers;
using System.Net.Http.Json;
using DocumentManager.Domain.Constants;
using DocumentManager.Domain.Entities;
using DocumentManager.Infrastructure.Persistence;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace DocumentManager.IntegrationTests;

public sealed class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    private const string SigningKey = "local-test-signing-key-with-more-than-32-characters";
    private readonly string _databaseName = $"DocumentManagerTests-{Guid.NewGuid():N}";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureAppConfiguration(configuration =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Issuer"] = "DocumentManager.Tests",
                ["Jwt:Audience"] = "DocumentManager.Tests",
                ["Jwt:SigningKey"] = SigningKey,
                ["DevelopmentSeed:AdminEmail"] = "admin@test.local",
                ["DevelopmentSeed:AdminPassword"] = "AdminPassword123",
                ["DevelopmentSeed:AdminFirstName"] = "Admin",
                ["DevelopmentSeed:AdminLastName"] = "Tests",
                ["ConnectionStrings:DefaultConnection"] = "InMemory"
            });
        });

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();
            services.RemoveAll<IDbContextOptionsConfiguration<ApplicationDbContext>>();
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseInMemoryDatabase(_databaseName));
        });
    }

    public async Task<string> LoginAndGetAccessTokenAsync(string username = "admin@test.local", string password = "AdminPassword123")
    {
        if (username == "admin@test.local")
        {
            await SeedUserAsync(username, password, AppRoles.Administrator);
        }

        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        var response = await client.PostAsJsonAsync("/api/auth/login", new { usernameOrEmail = username, password });
        response.EnsureSuccessStatusCode();
        var payload = await response.Content.ReadFromJsonAsync<LoginPayload>();
        return payload?.AccessToken ?? throw new InvalidOperationException("Login did not return an access token.");
    }

    public async Task SeedUserAsync(string email, string password, string role, bool isActive = true)
    {
        using var scope = Services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole(role));
        }

        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var existing = await userManager.FindByEmailAsync(email);
        if (existing is not null)
        {
            return;
        }

        var user = new ApplicationUser
        {
            UserName = email,
            Email = email,
            FirstName = "Test",
            LastName = "User",
            EmailConfirmed = true,
            IsActive = isActive,
            CreatedAt = DateTimeOffset.UtcNow
        };
        var result = await userManager.CreateAsync(user, password);
        if (!result.Succeeded)
        {
            throw new InvalidOperationException(string.Join(" ", result.Errors.Select(error => error.Description)));
        }

        await userManager.AddToRoleAsync(user, role);
    }

    public HttpClient CreateAuthenticatedClient(string accessToken)
    {
        var client = CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
        return client;
    }

    private sealed record LoginPayload(string AccessToken);
}
