using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace NoorLocator.IntegrationTests;

public class Phase8EndpointsTests(NoorLocatorWebApplicationFactory factory) : IClassFixture<NoorLocatorWebApplicationFactory>
{
    [Fact]
    public async Task EventAnnouncements_PublicFeed_ExcludesDraftsButManagerCanSeeThem()
    {
        using var managerClient = factory.CreateClient();
        using var publicClient = factory.CreateClient();

        var token = await LoginAsync(managerClient, "manager@test.local", "Manager123!Pass");
        managerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        await CreateAnnouncementAsync(managerClient, "Published integration announcement", "Published");
        await CreateAnnouncementAsync(managerClient, "Draft integration announcement", "Draft");

        var publicResponse = await publicClient.GetAsync("/api/event-announcements?centerId=1");
        var publicPayload = await publicResponse.Content.ReadFromJsonAsync<ApiEnvelope<List<EventAnnouncementPayload>>>();

        Assert.Equal(HttpStatusCode.OK, publicResponse.StatusCode);
        Assert.NotNull(publicPayload?.Data);
        Assert.Contains(publicPayload.Data!, announcement => announcement.Title == "Published integration announcement");
        Assert.DoesNotContain(publicPayload.Data!, announcement => announcement.Title == "Draft integration announcement");

        var managerResponse = await managerClient.GetAsync("/api/event-announcements?centerId=1");
        var managerPayload = await managerResponse.Content.ReadFromJsonAsync<ApiEnvelope<List<EventAnnouncementPayload>>>();

        Assert.Equal(HttpStatusCode.OK, managerResponse.StatusCode);
        Assert.NotNull(managerPayload?.Data);
        Assert.Contains(managerPayload.Data!, announcement => announcement.Title == "Published integration announcement");
        Assert.Contains(managerPayload.Data!, announcement => announcement.Title == "Draft integration announcement");
    }

    [Fact]
    public async Task CenterImages_ManagerUpload_ReturnsImageInPublicGallery()
    {
        using var managerClient = factory.CreateClient();
        using var publicClient = factory.CreateClient();

        var token = await LoginAsync(managerClient, "manager@test.local", "Manager123!Pass");
        managerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var uploadContent = new MultipartFormDataContent
        {
            { new StringContent("1"), "CenterId" },
            { new StringContent("true"), "IsPrimary" }
        };

        var imageContent = new ByteArrayContent(Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO5+9FoAAAAASUVORK5CYII="));
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        uploadContent.Add(imageContent, "Image", "integration-center.png");

        var uploadResponse = await managerClient.PostAsync("/api/center-images/upload", uploadContent);
        var uploadPayload = await uploadResponse.Content.ReadFromJsonAsync<ApiEnvelope<CenterImagePayload>>();

        Assert.Equal(HttpStatusCode.Created, uploadResponse.StatusCode);
        Assert.NotNull(uploadPayload?.Data);
        Assert.True(uploadPayload.Data!.IsPrimary);

        var galleryResponse = await publicClient.GetAsync("/api/centers/1/images");
        var galleryPayload = await galleryResponse.Content.ReadFromJsonAsync<ApiEnvelope<List<CenterImagePayload>>>();

        Assert.Equal(HttpStatusCode.OK, galleryResponse.StatusCode);
        Assert.NotNull(galleryPayload?.Data);
        Assert.Contains(galleryPayload.Data!, image => image.Id == uploadPayload.Data.Id && image.IsPrimary);
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<LoginPayload>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload?.Data?.Token);
        return payload.Data.Token;
    }

    private static async Task CreateAnnouncementAsync(HttpClient client, string title, string status)
    {
        using var content = new MultipartFormDataContent
        {
            { new StringContent(title), "Title" },
            { new StringContent("Integration announcement body."), "Description" },
            { new StringContent("1"), "CenterId" },
            { new StringContent(status), "Status" }
        };

        var response = await client.PostAsync("/api/event-announcements", content);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }
}

public sealed class EventAnnouncementPayload
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;
}

public sealed class CenterImagePayload
{
    public int Id { get; set; }

    public bool IsPrimary { get; set; }
}
