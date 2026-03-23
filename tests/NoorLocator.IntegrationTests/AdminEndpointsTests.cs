using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace NoorLocator.IntegrationTests;

public class AdminEndpointsTests(NoorLocatorWebApplicationFactory factory) : IClassFixture<NoorLocatorWebApplicationFactory>
{
    [Fact]
    public async Task Dashboard_ReturnsForbiddenForNonAdmin()
    {
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "user@test.local", "User123!Pass");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/admin/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_ReturnsMetricsForAdmin()
    {
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "admin@test.local", "Admin123!Pass");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/admin/dashboard");
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<AdminDashboardPayload>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.NotNull(payload.Data);
        Assert.True(payload.Data!.TotalUsers >= 3);
        Assert.True(payload.Data.TotalCenters >= 2);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<LoginPayload>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload?.Data?.Token);
        return payload.Data.Token;
    }
}

public sealed class AdminDashboardPayload
{
    public int TotalUsers { get; set; }

    public int TotalCenters { get; set; }
}

public sealed class LoginPayload
{
    public string Token { get; set; } = string.Empty;
}
