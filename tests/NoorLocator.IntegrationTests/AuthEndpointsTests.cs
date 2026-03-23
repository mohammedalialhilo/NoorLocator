using System.Net;
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
}

public sealed class ApiEnvelope<T>
{
    public bool Success { get; set; }

    public string Message { get; set; } = string.Empty;

    public T? Data { get; set; }
}

public sealed class AuthPayload
{
    public string Role { get; set; } = string.Empty;

    public CurrentUserPayload? User { get; set; }
}

public sealed class CurrentUserPayload
{
    public string Email { get; set; } = string.Empty;
}
