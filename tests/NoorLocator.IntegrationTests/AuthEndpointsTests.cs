using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace NoorLocator.IntegrationTests;

public class AuthEndpointsTests(NoorLocatorWebApplicationFactory factory) : IClassFixture<NoorLocatorWebApplicationFactory>
{
    [Fact]
    public async Task Register_ReturnsCreatedUserWithUserRole()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/auth/register", new
        {
            name = "New Integration User",
            email = "new-user@test.local",
            password = "Secure123!"
        });

        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<AuthPayload>>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.NotNull(payload.Data);
        Assert.Equal("User", payload.Data!.Role);
        Assert.Equal("new-user@test.local", payload.Data.User!.Email);
    }

    [Fact]
    public async Task Login_ThenMe_ReturnsAuthenticatedProfile()
    {
        using var client = factory.CreateClient();

        var auth = await LoginAsync(client, "user@test.local", "User123!Pass");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var response = await client.GetAsync("/api/auth/me");
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<CurrentUserPayload>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload?.Data);
        Assert.Equal("user@test.local", payload.Data!.Email);
    }

    [Fact]
    public async Task Logout_RevokesCurrentSession_AndProtectedEndpointReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var auth = await LoginAsync(client, "user@test.local", "User123!Pass");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var logoutResponse = await client.PostAsJsonAsync("/api/auth/logout", new
        {
            refreshToken = auth.RefreshToken
        });

        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        var meResponse = await client.GetAsync("/api/auth/me");
        Assert.Equal(HttpStatusCode.Unauthorized, meResponse.StatusCode);
    }

    private static async Task<AuthPayload> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<AuthPayload>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload?.Data);
        Assert.False(string.IsNullOrWhiteSpace(payload!.Data!.Token));
        Assert.False(string.IsNullOrWhiteSpace(payload.Data.RefreshToken));
        return payload.Data;
    }
}

public sealed class ApiEnvelope<T>
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public T? Data { get; set; }
}

public sealed class AuthPayload
{
    public string Token { get; set; } = string.Empty;

    public string RefreshToken { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public CurrentUserPayload? User { get; set; }
}

public sealed class CurrentUserPayload
{
    public string Email { get; set; } = string.Empty;
}
