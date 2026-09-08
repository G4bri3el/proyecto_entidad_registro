using System.Net;
using System.Net.Http.Json;
using DocumentManager.Domain.Constants;

namespace DocumentManager.IntegrationTests;

public sealed class AuthAndAuthorizationTests(CustomWebApplicationFactory factory) : IClassFixture<CustomWebApplicationFactory>
{
    [Fact]
    public async Task Anonymous_user_cannot_access_folders_tree()
    {
        var client = factory.CreateClient();

        var response = await client.GetAsync("/api/folders/tree");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_returns_access_token_for_development_admin()
    {
        var token = await factory.LoginAndGetAccessTokenAsync();

        Assert.False(string.IsNullOrWhiteSpace(token));
    }

    [Fact]
    public async Task Viewer_cannot_upload_documents()
    {
        await factory.SeedUserAsync("viewer@test.local", "ViewerPassword123", AppRoles.Viewer);
        var token = await factory.LoginAndGetAccessTokenAsync("viewer@test.local", "ViewerPassword123");
        var client = factory.CreateAuthenticatedClient(token);
        using var multipart = new MultipartFormDataContent();
        multipart.Add(new ByteArrayContent("%PDF-1.7 test"u8.ToArray()), "file", "contrato.pdf");

        var response = await client.PostAsync($"/api/folders/{Guid.NewGuid()}/documents", multipart);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Disabled_user_cannot_login()
    {
        await factory.SeedUserAsync("disabled@test.local", "DisabledPassword123", AppRoles.Viewer, isActive: false);
        var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/login", new { usernameOrEmail = "disabled@test.local", password = "DisabledPassword123" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Audit_logs_do_not_expose_public_mutation_endpoints()
    {
        var token = await factory.LoginAndGetAccessTokenAsync();
        var client = factory.CreateAuthenticatedClient(token);

        var putResponse = await client.PutAsJsonAsync($"/api/audit/{Guid.NewGuid()}", new { description = "tamper" });
        var deleteResponse = await client.DeleteAsync($"/api/audit/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.MethodNotAllowed, putResponse.StatusCode);
        Assert.Equal(HttpStatusCode.MethodNotAllowed, deleteResponse.StatusCode);
    }
}
