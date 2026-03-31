using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using NoorLocator.Domain.Enums;
using NoorLocator.Infrastructure.Persistence;

namespace NoorLocator.IntegrationTests;

public class ProfileEndpointsTests
{
    [Fact]
    public async Task AuthenticatedUser_CanViewAndUpdateOwnProfile_AndChangesPersist()
    {
        using var factory = new NoorLocatorWebApplicationFactory();
        using var client = factory.CreateClient();

        var auth = await LoginAsync(client, "user@test.local", "User123!Pass");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var initialProfileResponse = await client.GetAsync("/api/profile/me");
        var initialProfile = await initialProfileResponse.Content.ReadFromJsonAsync<ApiEnvelope<ProfilePayload>>();

        Assert.Equal(HttpStatusCode.OK, initialProfileResponse.StatusCode);
        Assert.NotNull(initialProfile?.Data);
        Assert.Equal("Integration User", initialProfile.Data!.Name);
        Assert.Equal("user@test.local", initialProfile.Data.Email);
        Assert.Equal("User", initialProfile.Data.Role);

        var updatedName = $"Updated User {Guid.NewGuid():N}"[..24];
        var updatedEmail = $"profile-{Guid.NewGuid():N}@test.local";

        var updateResponse = await client.PutAsJsonAsync("/api/profile/me", new
        {
            name = updatedName,
            email = updatedEmail
        });
        var updatePayload = await updateResponse.Content.ReadFromJsonAsync<ApiEnvelope<ProfilePayload>>();

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updatePayload?.Data);
        Assert.Equal(updatedName, updatePayload.Data!.Name);
        Assert.Equal(updatedEmail, updatePayload.Data.Email);
        Assert.Equal("User", updatePayload.Data.Role);
        Assert.False(updatePayload.Data.IsEmailVerified);

        var refreshedProfileResponse = await client.GetAsync("/api/profile/me");
        var refreshedProfile = await refreshedProfileResponse.Content.ReadFromJsonAsync<ApiEnvelope<ProfilePayload>>();
        Assert.Equal(HttpStatusCode.OK, refreshedProfileResponse.StatusCode);
        Assert.Equal(updatedName, refreshedProfile?.Data?.Name);
        Assert.Equal(updatedEmail, refreshedProfile?.Data?.Email);

        var authMeResponse = await client.GetAsync("/api/auth/me");
        var authMePayload = await authMeResponse.Content.ReadFromJsonAsync<ApiEnvelope<ProfilePayload>>();
        Assert.Equal(HttpStatusCode.OK, authMeResponse.StatusCode);
        Assert.Equal(updatedName, authMePayload?.Data?.Name);
        Assert.Equal(updatedEmail, authMePayload?.Data?.Email);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NoorLocatorDbContext>();
            var storedUser = dbContext.Users.Single(user => user.Id == 2);
            Assert.Equal(updatedName, storedUser.Name);
            Assert.Equal(updatedEmail, storedUser.Email);
            Assert.Equal(UserRole.User, storedUser.Role);
            Assert.False(storedUser.IsEmailVerified);
            Assert.False(string.IsNullOrWhiteSpace(storedUser.EmailVerificationTokenHash));
        }

        using var reloginClient = factory.CreateClient();
        var reloginResponse = await reloginClient.PostAsJsonAsync("/api/auth/login", new
        {
            email = updatedEmail,
            password = "User123!Pass"
        });
        var reloginPayload = await reloginResponse.Content.ReadFromJsonAsync<ApiEnvelope<AuthPayload>>();
        Assert.Equal(HttpStatusCode.Forbidden, reloginResponse.StatusCode);
        Assert.Equal("Please verify your email before signing in.", reloginPayload?.Message);
    }

    [Fact]
    public async Task ProfileEndpoints_RequireAuthentication_AndRejectInvalidInput()
    {
        using var factory = new NoorLocatorWebApplicationFactory();
        using var anonymousClient = factory.CreateClient();

        var anonymousGetResponse = await anonymousClient.GetAsync("/api/profile/me");
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousGetResponse.StatusCode);

        var anonymousPutResponse = await anonymousClient.PutAsJsonAsync("/api/profile/me", new
        {
            name = "Anonymous",
            email = "anonymous@test.local"
        });
        Assert.Equal(HttpStatusCode.Unauthorized, anonymousPutResponse.StatusCode);

        using var client = factory.CreateClient();
        var auth = await LoginAsync(client, "user@test.local", "User123!Pass");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var invalidResponse = await client.PutAsJsonAsync("/api/profile/me", new
        {
            name = "",
            email = "not-an-email"
        });
        var invalidPayload = await invalidResponse.Content.ReadFromJsonAsync<ApiEnvelope<object>>();

        Assert.Equal(HttpStatusCode.BadRequest, invalidResponse.StatusCode);
        Assert.Equal("Display name is required.", invalidPayload?.Message);

        var duplicateEmailResponse = await client.PutAsJsonAsync("/api/profile/me", new
        {
            name = "Integration User",
            email = "admin@test.local"
        });
        var duplicateEmailPayload = await duplicateEmailResponse.Content.ReadFromJsonAsync<ApiEnvelope<object>>();

        Assert.Equal(HttpStatusCode.Conflict, duplicateEmailResponse.StatusCode);
        Assert.Equal("An account with this email already exists.", duplicateEmailPayload?.Message);
    }

    [Fact]
    public async Task ProfileUpdate_DoesNotModifyOtherUsersOrRestrictedFields()
    {
        using var factory = new NoorLocatorWebApplicationFactory();
        using var client = factory.CreateClient();

        var auth = await LoginAsync(client, "user@test.local", "User123!Pass");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        string adminNameBefore;
        string adminEmailBefore;
        string passwordHashBefore;

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NoorLocatorDbContext>();
            var adminUser = dbContext.Users.Single(user => user.Id == 1);
            var currentUser = dbContext.Users.Single(user => user.Id == 2);
            adminNameBefore = adminUser.Name;
            adminEmailBefore = adminUser.Email;
            passwordHashBefore = currentUser.PasswordHash;
        }

        var updatedEmail = $"restricted-{Guid.NewGuid():N}@test.local";
        using var request = new HttpRequestMessage(HttpMethod.Put, "/api/profile/me")
        {
            Content = new StringContent(
                $$"""
                {
                  "name": "Restricted Update User",
                  "email": "{{updatedEmail}}",
                  "role": "Admin",
                  "passwordHash": "InjectedHash",
                  "id": 1
                }
                """,
                Encoding.UTF8,
                "application/json")
        };

        var response = await client.SendAsync(request);
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<ProfilePayload>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload?.Data);
        Assert.Equal("Restricted Update User", payload.Data!.Name);
        Assert.Equal(updatedEmail, payload.Data.Email);
        Assert.Equal("User", payload.Data.Role);
        Assert.False(payload.Data.IsEmailVerified);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NoorLocatorDbContext>();
            var adminUser = dbContext.Users.Single(user => user.Id == 1);
            var currentUser = dbContext.Users.Single(user => user.Id == 2);

            Assert.Equal(adminNameBefore, adminUser.Name);
            Assert.Equal(adminEmailBefore, adminUser.Email);
            Assert.Equal(UserRole.User, currentUser.Role);
            Assert.Equal(passwordHashBefore, currentUser.PasswordHash);
            Assert.Equal("Restricted Update User", currentUser.Name);
            Assert.Equal(updatedEmail, currentUser.Email);
            Assert.False(currentUser.IsEmailVerified);
            Assert.False(string.IsNullOrWhiteSpace(currentUser.EmailVerificationTokenHash));
        }

        using var reloginClient = factory.CreateClient();
        var reloginResponse = await reloginClient.PostAsJsonAsync("/api/auth/login", new
        {
            email = updatedEmail,
            password = "User123!Pass"
        });
        var reloginPayload = await reloginResponse.Content.ReadFromJsonAsync<ApiEnvelope<AuthPayload>>();
        Assert.Equal(HttpStatusCode.Forbidden, reloginResponse.StatusCode);
        Assert.Equal("Please verify your email before signing in.", reloginPayload?.Message);
    }

    [Theory]
    [InlineData("manager@test.local", "Manager123!Pass", UserRole.Manager)]
    [InlineData("admin@test.local", "Admin123!Pass", UserRole.Admin)]
    public async Task ManagerAndAdmin_CanUpdateTheirOwnProfiles_WithoutChangingRoles(string email, string password, UserRole expectedRole)
    {
        using var factory = new NoorLocatorWebApplicationFactory();
        using var client = factory.CreateClient();

        var auth = await LoginAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var updatedName = $"{expectedRole} Updated {Guid.NewGuid():N}"[..24];
        var updateResponse = await client.PutAsJsonAsync("/api/profile/me", new
        {
            name = updatedName,
            email
        });
        var updatePayload = await updateResponse.Content.ReadFromJsonAsync<ApiEnvelope<ProfilePayload>>();

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updatePayload?.Data);
        Assert.Equal(updatedName, updatePayload.Data!.Name);
        Assert.Equal(expectedRole.ToString(), updatePayload.Data.Role);

        var authMeResponse = await client.GetAsync("/api/auth/me");
        var authMePayload = await authMeResponse.Content.ReadFromJsonAsync<ApiEnvelope<ProfilePayload>>();
        Assert.Equal(HttpStatusCode.OK, authMeResponse.StatusCode);
        Assert.Equal(updatedName, authMePayload?.Data?.Name);
        Assert.Equal(expectedRole.ToString(), authMePayload?.Data?.Role);

        using var scope = factory.Services.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<NoorLocatorDbContext>();
        var storedUser = dbContext.Users.Single(user => user.Email == email);
        Assert.Equal(updatedName, storedUser.Name);
        Assert.Equal(expectedRole, storedUser.Role);
    }

    private static async Task<AuthPayload> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<AuthPayload>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload?.Data);
        return payload!.Data!;
    }
}

public sealed class ProfilePayload
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsEmailVerified { get; set; }

    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public List<int> AssignedCenterIds { get; set; } = [];
}
