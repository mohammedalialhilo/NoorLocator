using System.Net;
using System.Net.Http.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NoorLocator.Infrastructure.Persistence;

namespace NoorLocator.IntegrationTests;

public class NotificationEndpointsTests
{
    [Fact]
    public async Task TrackVisit_And_Subscribe_DeduplicateEntries()
    {
        using var factory = new NoorLocatorWebApplicationFactory();
        using var userClient = factory.CreateClient();

        var userAuth = await IntegrationTestSupport.LoginAsync(userClient, "user@test.local", "User123!Pass");
        IntegrationTestSupport.Authorize(userClient, userAuth.Token);

        var firstVisitResponse = await userClient.PostAsJsonAsync("/api/centers/1/visit", new { source = "page_view" });
        await IntegrationTestSupport.ReadEnvelopeAsync<object?>(firstVisitResponse, HttpStatusCode.OK);

        var secondVisitResponse = await userClient.PostAsJsonAsync("/api/centers/1/visit", new { source = "page_view" });
        await IntegrationTestSupport.ReadEnvelopeAsync<object?>(secondVisitResponse, HttpStatusCode.OK);

        var firstSubscribeResponse = await userClient.PostAsync("/api/centers/1/subscribe", null);
        await IntegrationTestSupport.ReadEnvelopeAsync<CenterSubscriptionPayload>(firstSubscribeResponse, HttpStatusCode.OK);

        var secondSubscribeResponse = await userClient.PostAsync("/api/centers/1/subscribe", null);
        await IntegrationTestSupport.ReadEnvelopeAsync<CenterSubscriptionPayload>(secondSubscribeResponse, HttpStatusCode.OK);

        var subscriptionsResponse = await userClient.GetAsync("/api/users/me/subscriptions");
        var subscriptionsPayload = await IntegrationTestSupport.ReadEnvelopeAsync<List<CenterSubscriptionPayload>>(subscriptionsResponse, HttpStatusCode.OK);

        Assert.NotNull(subscriptionsPayload.Data);
        var subscription = Assert.Single(subscriptionsPayload.Data!);
        Assert.Equal(1, subscription.CenterId);
        Assert.True(subscription.IsEmailNotificationsEnabled);
        Assert.True(subscription.IsAppNotificationsEnabled);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NoorLocatorDbContext>();
            var visit = await dbContext.UserCenterVisits.SingleAsync(currentVisit => currentVisit.UserId == 2 && currentVisit.CenterId == 1);
            Assert.Equal(2, visit.VisitCount);

            var storedSubscription = await dbContext.UserCenterSubscriptions.SingleAsync(currentSubscription => currentSubscription.UserId == 2 && currentSubscription.CenterId == 1);
            Assert.True(storedSubscription.IsEmailNotificationsEnabled);
            Assert.True(storedSubscription.IsAppNotificationsEnabled);
        }

        var unsubscribeResponse = await userClient.DeleteAsync("/api/centers/1/subscribe");
        await IntegrationTestSupport.ReadEnvelopeAsync<object?>(unsubscribeResponse, HttpStatusCode.OK);

        var redundantUnsubscribeResponse = await userClient.DeleteAsync("/api/centers/1/subscribe");
        await IntegrationTestSupport.ReadEnvelopeAsync<object?>(redundantUnsubscribeResponse, HttpStatusCode.OK);

        using (var scope = factory.Services.CreateScope())
        {
            var dbContext = scope.ServiceProvider.GetRequiredService<NoorLocatorDbContext>();
            Assert.False(await dbContext.UserCenterSubscriptions.AnyAsync(currentSubscription => currentSubscription.UserId == 2 && currentSubscription.CenterId == 1));
        }
    }

    [Fact]
    public async Task VisitedOrSubscribedUser_ReceivesMajlisAndEventNotifications_AndCanMarkThemRead()
    {
        using var factory = new NoorLocatorWebApplicationFactory();
        var recorder = IntegrationTestSupport.GetEmailRecorder(factory);
        recorder.Clear();
        using var userClient = factory.CreateClient();
        using var managerClient = factory.CreateClient();

        var userAuth = await IntegrationTestSupport.LoginAsync(userClient, "user@test.local", "User123!Pass");
        IntegrationTestSupport.Authorize(userClient, userAuth.Token);

        var managerAuth = await IntegrationTestSupport.LoginAsync(managerClient, "manager@test.local", "Manager123!Pass");
        IntegrationTestSupport.Authorize(managerClient, managerAuth.Token);

        await IntegrationTestSupport.ReadEnvelopeAsync<object?>(
            await userClient.PostAsJsonAsync("/api/centers/1/visit", new { source = "page_view" }),
            HttpStatusCode.OK);
        await IntegrationTestSupport.ReadEnvelopeAsync<CenterSubscriptionPayload>(
            await userClient.PostAsync("/api/centers/1/subscribe", null),
            HttpStatusCode.OK);

        var majlisTitle = $"Majlis {Guid.NewGuid():N}"[..20];
        var createMajlisResponse = await managerClient.PostAsJsonAsync("/api/majalis", new
        {
            title = majlisTitle,
            description = "Integration notification majlis.",
            date = DateTime.UtcNow.Date.AddDays(7),
            time = "20:00",
            centerId = 1,
            languageIds = new[] { 1, 2 }
        });
        await IntegrationTestSupport.ReadEnvelopeAsync<object?>(createMajlisResponse, HttpStatusCode.Created);

        using var eventContent = new MultipartFormDataContent
        {
            { new StringContent($"Announcement {Guid.NewGuid():N}"[..24]), "Title" },
            { new StringContent("Integration notification announcement."), "Description" },
            { new StringContent("1"), "CenterId" },
            { new StringContent("Published"), "Status" }
        };
        var createEventResponse = await managerClient.PostAsync("/api/event-announcements", eventContent);
        await IntegrationTestSupport.ReadEnvelopeAsync<EventAnnouncementPayload>(createEventResponse, HttpStatusCode.Created);

        var notificationsResponse = await userClient.GetAsync("/api/notifications");
        var notificationsPayload = await IntegrationTestSupport.ReadEnvelopeAsync<List<NotificationPayload>>(notificationsResponse, HttpStatusCode.OK);

        Assert.NotNull(notificationsPayload.Data);
        Assert.Equal(2, notificationsPayload.Data!.Count);
        Assert.Contains(notificationsPayload.Data, notification => notification.Type == "Majlis" && notification.SentByEmail);
        Assert.Contains(notificationsPayload.Data, notification => notification.Type == "Event" && notification.SentByEmail);

        var unreadCountResponse = await userClient.GetAsync("/api/notifications/unread-count");
        var unreadCountPayload = await IntegrationTestSupport.ReadEnvelopeAsync<UnreadNotificationCountPayload>(unreadCountResponse, HttpStatusCode.OK);
        Assert.Equal(2, unreadCountPayload.Data!.Count);

        var firstNotificationId = notificationsPayload.Data[0].Id;
        var markReadResponse = await userClient.PutAsync($"/api/notifications/{firstNotificationId}/read", null);
        await IntegrationTestSupport.ReadEnvelopeAsync<object?>(markReadResponse, HttpStatusCode.OK);

        var unreadAfterSingleReadResponse = await userClient.GetAsync("/api/notifications/unread-count");
        var unreadAfterSingleReadPayload = await IntegrationTestSupport.ReadEnvelopeAsync<UnreadNotificationCountPayload>(unreadAfterSingleReadResponse, HttpStatusCode.OK);
        Assert.Equal(1, unreadAfterSingleReadPayload.Data!.Count);

        var markAllResponse = await userClient.PutAsync("/api/notifications/read-all", null);
        await IntegrationTestSupport.ReadEnvelopeAsync<object?>(markAllResponse, HttpStatusCode.OK);

        var unreadAfterMarkAllResponse = await userClient.GetAsync("/api/notifications/unread-count");
        var unreadAfterMarkAllPayload = await IntegrationTestSupport.ReadEnvelopeAsync<UnreadNotificationCountPayload>(unreadAfterMarkAllResponse, HttpStatusCode.OK);
        Assert.Equal(0, unreadAfterMarkAllPayload.Data!.Count);

        var userEmails = recorder.Snapshot()
            .Where(message => string.Equals(message.ToEmail, "user@test.local", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.Equal(2, userEmails.Length);
        Assert.All(userEmails, message => Assert.Equal("noorlocator@gmail.com", message.FromEmail));
        Assert.Contains(userEmails, message => message.Subject.Contains("New majlis at", StringComparison.Ordinal));
        Assert.Contains(userEmails, message => message.Subject.Contains("New event update from", StringComparison.Ordinal));
    }

    [Fact]
    public async Task NotificationPreferences_AreRespected_WhenEmailIsDisabledButInAppIsEnabled()
    {
        using var factory = new NoorLocatorWebApplicationFactory();
        var recorder = IntegrationTestSupport.GetEmailRecorder(factory);
        recorder.Clear();
        using var userClient = factory.CreateClient();
        using var managerClient = factory.CreateClient();

        var userAuth = await IntegrationTestSupport.LoginAsync(userClient, "user@test.local", "User123!Pass");
        IntegrationTestSupport.Authorize(userClient, userAuth.Token);

        var managerAuth = await IntegrationTestSupport.LoginAsync(managerClient, "manager@test.local", "Manager123!Pass");
        IntegrationTestSupport.Authorize(managerClient, managerAuth.Token);

        await IntegrationTestSupport.ReadEnvelopeAsync<object?>(
            await userClient.PostAsJsonAsync("/api/centers/1/visit", new { source = "page_view" }),
            HttpStatusCode.OK);
        await IntegrationTestSupport.ReadEnvelopeAsync<CenterSubscriptionPayload>(
            await userClient.PostAsync("/api/centers/1/subscribe", null),
            HttpStatusCode.OK);

        var preferencesResponse = await userClient.PutAsJsonAsync("/api/profile/me/notification-preferences", new
        {
            emailNotificationsEnabled = false,
            appNotificationsEnabled = true,
            majlisNotificationsEnabled = true,
            eventNotificationsEnabled = true,
            centerUpdatesEnabled = true
        });
        var preferencesPayload = await IntegrationTestSupport.ReadEnvelopeAsync<NotificationPreferencePayload>(preferencesResponse, HttpStatusCode.OK);
        Assert.NotNull(preferencesPayload.Data);
        Assert.False(preferencesPayload.Data!.EmailNotificationsEnabled);
        Assert.True(preferencesPayload.Data.AppNotificationsEnabled);

        using var eventContent = new MultipartFormDataContent
        {
            { new StringContent($"Pref event {Guid.NewGuid():N}"[..22]), "Title" },
            { new StringContent("Preference-respecting announcement."), "Description" },
            { new StringContent("1"), "CenterId" },
            { new StringContent("Published"), "Status" }
        };
        var createEventResponse = await managerClient.PostAsync("/api/event-announcements", eventContent);
        await IntegrationTestSupport.ReadEnvelopeAsync<EventAnnouncementPayload>(createEventResponse, HttpStatusCode.Created);

        var notificationsResponse = await userClient.GetAsync("/api/notifications");
        var notificationsPayload = await IntegrationTestSupport.ReadEnvelopeAsync<List<NotificationPayload>>(notificationsResponse, HttpStatusCode.OK);
        Assert.NotNull(notificationsPayload.Data);
        var notification = Assert.Single(notificationsPayload.Data!);
        Assert.Equal("Event", notification.Type);
        Assert.False(notification.SentByEmail);

        var unreadCountResponse = await userClient.GetAsync("/api/notifications/unread-count");
        var unreadCountPayload = await IntegrationTestSupport.ReadEnvelopeAsync<UnreadNotificationCountPayload>(unreadCountResponse, HttpStatusCode.OK);
        Assert.Equal(1, unreadCountPayload.Data!.Count);

        Assert.DoesNotContain(
            recorder.Snapshot(),
            message => string.Equals(message.ToEmail, "user@test.local", StringComparison.OrdinalIgnoreCase)
                       && message.Subject.Contains("New event update from", StringComparison.Ordinal));
    }
}
