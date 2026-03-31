using System.IdentityModel.Tokens.Jwt;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using NoorLocator.Infrastructure.Persistence;

namespace NoorLocator.IntegrationTests;

public class AuthEndpointsTests(NoorLocatorWebApplicationFactory factory) : IClassFixture<NoorLocatorWebApplicationFactory>
{
    [Fact]
    public async Task Register_CreatesUnverifiedUser_WithoutTrustedSession()
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
        Assert.False(payload.Data.User.IsEmailVerified);
        Assert.True(string.IsNullOrWhiteSpace(payload.Data.Token));
        Assert.True(string.IsNullOrWhiteSpace(payload.Data.RefreshToken));
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

    [Theory]
    [InlineData("user@test.local", "User123!Pass", "/api/auth/me")]
    [InlineData("manager@test.local", "Manager123!Pass", "/api/manager/my-centers")]
    [InlineData("admin@test.local", "Admin123!Pass", "/api/admin/dashboard")]
    public async Task Logout_RevokesCurrentSession_AndProtectedEndpointReturnsUnauthorized(string email, string password, string protectedPath)
    {
        using var client = factory.CreateClient();

        var auth = await LoginAsync(client, email, password);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", auth.Token);

        var logoutResponse = await client.PostAsJsonAsync("/api/auth/logout", new
        {
            refreshToken = auth.RefreshToken
        });

        Assert.Equal(HttpStatusCode.OK, logoutResponse.StatusCode);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NoorLocatorDbContext>();
            var sessionId = ReadSessionId(auth.Token);
            var revokedSession = await dbContext.RefreshTokens.SingleAsync(token => token.SessionId == sessionId);
            Assert.NotNull(revokedSession.RevokedAtUtc);
        }

        var protectedResponse = await client.GetAsync(protectedPath);
        Assert.Equal(HttpStatusCode.Unauthorized, protectedResponse.StatusCode);
    }

    [Fact]
    public async Task ExpiredAccessToken_ReturnsUnauthorized()
    {
        using var client = factory.CreateClient();

        var auth = await LoginAsync(client, "user@test.local", "User123!Pass");
        var expiredToken = CreateExpiredAccessToken(factory, auth.Token, "user@test.local", "Integration User", "User", 2);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", expiredToken);

        var response = await client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
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

    private static string ReadSessionId(string accessToken)
    {
        return new JwtSecurityTokenHandler()
            .ReadJwtToken(accessToken)
            .Claims
            .Single(claim => claim.Type == JwtRegisteredClaimNames.Sid)
            .Value;
    }

    private static string CreateExpiredAccessToken(
        NoorLocatorWebApplicationFactory factory,
        string activeAccessToken,
        string email,
        string name,
        string role,
        int userId)
    {
        using var scope = factory.Services.CreateScope();
        var configuration = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var issuer = configuration["Jwt:Issuer"] ?? "NoorLocator";
        var audience = configuration["Jwt:Audience"] ?? "NoorLocator.Client";
        var configuredKey = configuration["Jwt:Key"] ?? "CHANGE-ME-TO-A-SECURE-32-CHARACTER-MINIMUM-SECRET";
        var normalizedKey = configuredKey.Length >= 32 ? configuredKey : configuredKey.PadRight(32, '_');
        var sessionId = ReadSessionId(activeAccessToken);
        var now = DateTime.UtcNow;

        var claims = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, userId.ToString()),
            new(ClaimTypes.NameIdentifier, userId.ToString()),
            new(ClaimTypes.Email, email),
            new(ClaimTypes.Name, name),
            new(ClaimTypes.Role, role),
            new(JwtRegisteredClaimNames.Sid, sessionId),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            notBefore: now.AddHours(-2),
            expires: now.AddMinutes(-10),
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(normalizedKey)),
                SecurityAlgorithms.HmacSha256));

        return new JwtSecurityTokenHandler().WriteToken(token);
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

    public bool IsEmailVerified { get; set; }
}
