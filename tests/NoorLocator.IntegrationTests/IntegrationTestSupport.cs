using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using NoorLocator.Infrastructure.Services.Email;

namespace NoorLocator.IntegrationTests;

internal static class IntegrationTestSupport
{
    private static readonly Regex TokenRegex = new(@"[?&]token=([^&""'\s<>]+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static EmailDispatchRecorder GetEmailRecorder(NoorLocatorWebApplicationFactory factory)
    {
        return factory.Services.GetRequiredService<EmailDispatchRecorder>();
    }

    public static async Task<AuthPayload> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var payload = await ReadEnvelopeAsync<AuthPayload>(response, HttpStatusCode.OK);

        Assert.NotNull(payload.Data);
        Assert.False(string.IsNullOrWhiteSpace(payload.Data!.Token));
        Assert.False(string.IsNullOrWhiteSpace(payload.Data.RefreshToken));
        return payload.Data;
    }

    public static void Authorize(HttpClient client, string token)
    {
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }

    public static async Task<ApiEnvelope<T>> ReadEnvelopeAsync<T>(HttpResponseMessage response, HttpStatusCode expectedStatus)
    {
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>();

        Assert.Equal(expectedStatus, response.StatusCode);
        Assert.NotNull(payload);
        return payload!;
    }

    public static string ExtractToken(EmailDispatchMessage message)
    {
        var match = TokenRegex.Match(message.HtmlBody);
        if (!match.Success)
        {
            match = TokenRegex.Match(message.TextBody);
        }

        Assert.True(match.Success, $"No token could be found in the email body for '{message.Subject}'.");
        return Uri.UnescapeDataString(match.Groups[1].Value);
    }
}

public sealed class VerifyEmailPayload
{
    public string Status { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;
}

public sealed class ProfileDetailsPayload
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public bool IsEmailVerified { get; set; }

    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public int[] AssignedCenterIds { get; set; } = [];
}

public sealed class CenterSubscriptionPayload
{
    public int CenterId { get; set; }

    public string CenterName { get; set; } = string.Empty;

    public bool IsEmailNotificationsEnabled { get; set; }

    public bool IsAppNotificationsEnabled { get; set; }

    public DateTime CreatedAt { get; set; }
}

public sealed class NotificationPayload
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public string RelatedEntityType { get; set; } = string.Empty;

    public int? RelatedEntityId { get; set; }

    public string? LinkUrl { get; set; }

    public bool IsRead { get; set; }

    public DateTime CreatedAt { get; set; }

    public bool SentByEmail { get; set; }
}

public sealed class UnreadNotificationCountPayload
{
    public int Count { get; set; }
}

public sealed class NotificationPreferencePayload
{
    public bool EmailNotificationsEnabled { get; set; }

    public bool AppNotificationsEnabled { get; set; }

    public bool MajlisNotificationsEnabled { get; set; }

    public bool EventNotificationsEnabled { get; set; }

    public bool CenterUpdatesEnabled { get; set; }
}
