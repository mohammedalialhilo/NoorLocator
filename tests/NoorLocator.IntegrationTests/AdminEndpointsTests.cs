using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace NoorLocator.IntegrationTests;

public class AdminEndpointsTests
{
    [Fact]
    public async Task Dashboard_ReturnsForbiddenForNonAdmin()
    {
        using var factory = new NoorLocatorWebApplicationFactory();
        using var client = factory.CreateClient();
        var token = await LoginAsync(client, "user@test.local", "User123!Pass");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await client.GetAsync("/api/admin/dashboard");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Dashboard_ReturnsMetricsForAdmin()
    {
        using var factory = new NoorLocatorWebApplicationFactory();
        using var client = await CreateAuthenticatedClientAsync(factory, "admin@test.local", "Admin123!Pass");

        var response = await client.GetAsync("/api/admin/dashboard");
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<AdminDashboardPayload>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.NotNull(payload.Data);
        Assert.True(payload.Data!.TotalUsers >= 4);
        Assert.True(payload.Data.TotalCenters >= 2);
    }

    [Fact]
    public async Task UserManagementEndpoints_ReturnForbiddenForNonAdmin()
    {
        using var factory = new NoorLocatorWebApplicationFactory();
        using var client = await CreateAuthenticatedClientAsync(factory, "user@test.local", "User123!Pass");

        var requests = new[]
        {
            new HttpRequestMessage(HttpMethod.Get, "/api/admin/users"),
            new HttpRequestMessage(HttpMethod.Get, "/api/admin/users/3"),
            CreateJsonRequest(HttpMethod.Put, "/api/admin/users/4", new
            {
                name = "Blocked",
                email = "blocked@test.local",
                role = "User",
                preferredLanguageCode = "en"
            }),
            new HttpRequestMessage(HttpMethod.Delete, "/api/admin/users/4"),
            new HttpRequestMessage(HttpMethod.Get, "/api/admin/manager-assignments"),
            CreateJsonRequest(HttpMethod.Post, "/api/admin/manager-assignments", new
            {
                userId = 2,
                centerId = 2
            }),
            CreateJsonRequest(HttpMethod.Put, "/api/admin/manager-assignments/1", new
            {
                userId = 3,
                centerId = 2
            }),
            new HttpRequestMessage(HttpMethod.Delete, "/api/admin/manager-assignments/1")
        };

        foreach (var request in requests)
        {
            var response = await client.SendAsync(request);
            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }

    [Fact]
    public async Task Admin_CanListViewUpdateAndDeleteUsers()
    {
        using var factory = new NoorLocatorWebApplicationFactory();
        using var client = await CreateAuthenticatedClientAsync(factory, "admin@test.local", "Admin123!Pass");

        var usersPayload = await ReadEnvelopeAsync<List<AdminUserListPayload>>(client, "/api/admin/users");
        var cleanupUser = Assert.Single(usersPayload, user => user.Email == "cleanup@test.local");
        Assert.True(cleanupUser.CanDelete);

        var managerDetails = await ReadEnvelopeAsync<AdminUserDetailsPayload>(client, "/api/admin/users/3");
        Assert.Equal("manager@test.local", managerDetails.Email);
        Assert.Equal("Manager", managerDetails.Role);
        Assert.True(managerDetails.ManagedCenters.Count >= 1);
        Assert.True(managerDetails.CreatedMajalis.Count >= 1);

        var updateResponse = await client.PutAsJsonAsync($"/api/admin/users/{cleanupUser.Id}", new
        {
            name = "Cleanup Candidate Updated",
            email = "cleanup.updated@test.local",
            role = "Manager",
            preferredLanguageCode = "ar"
        });
        var updatePayload = await updateResponse.Content.ReadFromJsonAsync<ApiEnvelope<AdminUserDetailsPayload>>();

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updatePayload?.Data);
        Assert.Equal("Cleanup Candidate Updated", updatePayload!.Data!.Name);
        Assert.Equal("cleanup.updated@test.local", updatePayload.Data.Email);
        Assert.Equal("Manager", updatePayload.Data.Role);
        Assert.Equal("ar", updatePayload.Data.PreferredLanguageCode);

        var deleteResponse = await client.DeleteAsync($"/api/admin/users/{cleanupUser.Id}");
        var deletePayload = await deleteResponse.Content.ReadFromJsonAsync<ApiEnvelope<object>>();

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.NotNull(deletePayload);
        Assert.True(deletePayload!.Success);

        var refreshedUsers = await ReadEnvelopeAsync<List<AdminUserListPayload>>(client, "/api/admin/users");
        Assert.DoesNotContain(refreshedUsers, user => user.Id == cleanupUser.Id);
    }

    [Fact]
    public async Task Admin_CanCreateUpdateAndDeleteManagerAssignments_AndManagerAccessChanges()
    {
        using var factory = new NoorLocatorWebApplicationFactory();
        using var adminClient = await CreateAuthenticatedClientAsync(factory, "admin@test.local", "Admin123!Pass");

        var createResponse = await adminClient.PostAsJsonAsync("/api/admin/manager-assignments", new
        {
            userId = 2,
            centerId = 2
        });
        var createPayload = await createResponse.Content.ReadFromJsonAsync<ApiEnvelope<AdminManagerAssignmentPayload>>();

        Assert.Equal(HttpStatusCode.OK, createResponse.StatusCode);
        Assert.NotNull(createPayload?.Data);
        Assert.Equal(2, createPayload!.Data!.UserId);
        Assert.Equal(2, createPayload.Data.CenterId);

        using var promotedManagerClient = await CreateAuthenticatedClientAsync(factory, "user@test.local", "User123!Pass");
        var managedCentersAfterCreate = await ReadEnvelopeAsync<List<ManagedCenterPayload>>(promotedManagerClient, "/api/manager/my-centers");
        Assert.Contains(managedCentersAfterCreate, center => center.Id == 2);

        var initialMajlisCreate = await promotedManagerClient.PostAsJsonAsync("/api/majalis", new
        {
            title = "Stockholm Assignment Majlis",
            description = "Created after admin assignment.",
            date = DateTime.UtcNow.Date.AddDays(7),
            time = "18:30",
            centerId = 2,
            languageIds = new[] { 2 }
        });

        Assert.Equal(HttpStatusCode.Created, initialMajlisCreate.StatusCode);

        var assignments = await ReadEnvelopeAsync<List<AdminManagerAssignmentPayload>>(adminClient, "/api/admin/manager-assignments");
        var userAssignment = Assert.Single(assignments, assignment => assignment.UserId == 2 && assignment.CenterId == 2);

        var updateResponse = await adminClient.PutAsJsonAsync($"/api/admin/manager-assignments/{userAssignment.Id}", new
        {
            userId = 2,
            centerId = 1
        });
        var updatePayload = await updateResponse.Content.ReadFromJsonAsync<ApiEnvelope<AdminManagerAssignmentPayload>>();

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updatePayload?.Data);
        Assert.Equal(1, updatePayload!.Data!.CenterId);

        using var reassignedManagerClient = await CreateAuthenticatedClientAsync(factory, "user@test.local", "User123!Pass");
        var managedCentersAfterUpdate = await ReadEnvelopeAsync<List<ManagedCenterPayload>>(reassignedManagerClient, "/api/manager/my-centers");
        Assert.Contains(managedCentersAfterUpdate, center => center.Id == 1);
        Assert.DoesNotContain(managedCentersAfterUpdate, center => center.Id == 2);

        var reassignedMajlisCreate = await reassignedManagerClient.PostAsJsonAsync("/api/majalis", new
        {
            title = "Copenhagen Assignment Majlis",
            description = "Created after reassignment.",
            date = DateTime.UtcNow.Date.AddDays(9),
            time = "19:15",
            centerId = 1,
            languageIds = new[] { 1, 2 }
        });
        Assert.Equal(HttpStatusCode.Created, reassignedMajlisCreate.StatusCode);

        var blockedMajlisCreate = await reassignedManagerClient.PostAsJsonAsync("/api/majalis", new
        {
            title = "Old Center Blocked Majlis",
            description = "This should now be rejected.",
            date = DateTime.UtcNow.Date.AddDays(10),
            time = "20:00",
            centerId = 2,
            languageIds = new[] { 2 }
        });
        Assert.Equal(HttpStatusCode.Forbidden, blockedMajlisCreate.StatusCode);

        var deleteResponse = await adminClient.DeleteAsync($"/api/admin/manager-assignments/{userAssignment.Id}");
        var deletePayload = await deleteResponse.Content.ReadFromJsonAsync<ApiEnvelope<object>>();

        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.NotNull(deletePayload);
        Assert.True(deletePayload!.Success);

        using var downgradedUserClient = await CreateAuthenticatedClientAsync(factory, "user@test.local", "User123!Pass");
        var downgradedCentersResponse = await downgradedUserClient.GetAsync("/api/manager/my-centers");
        Assert.Equal(HttpStatusCode.Forbidden, downgradedCentersResponse.StatusCode);

        var blockedAfterDeleteMajlisCreate = await downgradedUserClient.PostAsJsonAsync("/api/majalis", new
        {
            title = "No Assignment Remaining",
            description = "This should stay blocked.",
            date = DateTime.UtcNow.Date.AddDays(12),
            time = "18:00",
            centerId = 1,
            languageIds = new[] { 2 }
        });
        Assert.Equal(HttpStatusCode.Forbidden, blockedAfterDeleteMajlisCreate.StatusCode);
    }

    private static HttpRequestMessage CreateJsonRequest(HttpMethod method, string path, object payload)
    {
        return new HttpRequestMessage(method, path)
        {
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };
    }

    private static async Task<HttpClient> CreateAuthenticatedClientAsync(NoorLocatorWebApplicationFactory factory, string email, string password)
    {
        var client = factory.CreateClient();
        var token = await LoginAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<LoginPayload>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload?.Data?.Token);
        return payload.Data.Token;
    }

    private static async Task<T> ReadEnvelopeAsync<T>(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.NotNull(payload!.Data);
        return payload.Data!;
    }
}

public sealed class AdminDashboardPayload
{
    public int TotalUsers { get; set; }

    public int TotalCenters { get; set; }
}

public sealed class AdminUserListPayload
{
    public int Id { get; set; }

    public string Email { get; set; } = string.Empty;

    public bool CanDelete { get; set; }
}

public sealed class AdminUserDetailsPayload
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public string PreferredLanguageCode { get; set; } = string.Empty;

    public List<AdminManagedCenterPayload> ManagedCenters { get; set; } = [];

    public List<AdminManagedMajlisPayload> CreatedMajalis { get; set; } = [];
}

public sealed class AdminManagedCenterPayload
{
    public int AssignmentId { get; set; }

    public int CenterId { get; set; }
}

public sealed class AdminManagedMajlisPayload
{
    public int Id { get; set; }
}

public sealed class AdminManagerAssignmentPayload
{
    public int Id { get; set; }

    public int UserId { get; set; }

    public int CenterId { get; set; }
}

public sealed class ManagedCenterPayload
{
    public int Id { get; set; }
}

public sealed class LoginPayload
{
    public string Token { get; set; } = string.Empty;
}
