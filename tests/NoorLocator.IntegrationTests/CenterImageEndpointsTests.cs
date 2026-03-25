using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace NoorLocator.IntegrationTests;

public class CenterImageEndpointsTests(NoorLocatorWebApplicationFactory factory) : IClassFixture<NoorLocatorWebApplicationFactory>
{
    private static readonly byte[] ValidPngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO5+9FoAAAAASUVORK5CYII=");

    [Fact]
    public async Task ManagerUpload_StoresImage_AndPublicGalleryServesIt()
    {
        using var managerClient = factory.CreateClient();
        using var publicClient = factory.CreateClient();

        var token = await LoginAsync(managerClient, "manager@test.local", "Manager123!Pass");
        managerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uploadPayload = await UploadImageAsync(managerClient, centerId: 1, fileName: "gallery-public.png", ValidPngBytes, isPrimary: true);

        var galleryResponse = await publicClient.GetAsync("/api/centers/1/images");
        var galleryEnvelope = await galleryResponse.Content.ReadFromJsonAsync<ApiEnvelope<List<GalleryCenterImagePayload>>>();

        Assert.Equal(HttpStatusCode.OK, galleryResponse.StatusCode);
        Assert.NotNull(galleryEnvelope?.Data);
        var uploadedImage = Assert.Single(galleryEnvelope.Data!, image => image.Id == uploadPayload.Id);
        Assert.True(uploadedImage.IsPrimary);
        Assert.False(string.IsNullOrWhiteSpace(uploadedImage.ImageUrl));

        var imageResponse = await publicClient.GetAsync(uploadedImage.ImageUrl);
        Assert.Equal(HttpStatusCode.OK, imageResponse.StatusCode);
        Assert.StartsWith("image/", imageResponse.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task ManagerUpload_InvalidFileType_ReturnsBadRequest()
    {
        using var managerClient = factory.CreateClient();

        var token = await LoginAsync(managerClient, "manager@test.local", "Manager123!Pass");
        managerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var uploadContent = new MultipartFormDataContent
        {
            { new StringContent("1"), "CenterId" },
            { new StringContent("false"), "IsPrimary" }
        };
        var invalidContent = new ByteArrayContent("not-an-image"u8.ToArray());
        invalidContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        uploadContent.Add(invalidContent, "Image", "invalid.txt");

        var response = await managerClient.PostAsync("/api/center-images/upload", uploadContent);
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Only JPG, JPEG, PNG, and WEBP files are allowed.", payload?.Message);
    }

    [Fact]
    public async Task ManagerUpload_OversizedImage_ReturnsBadRequest()
    {
        using var managerClient = factory.CreateClient();

        var token = await LoginAsync(managerClient, "manager@test.local", "Manager123!Pass");
        managerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var uploadContent = new MultipartFormDataContent
        {
            { new StringContent("1"), "CenterId" },
            { new StringContent("false"), "IsPrimary" }
        };
        var oversizedContent = new byte[(5 * 1024 * 1024) + 1];
        oversizedContent[0] = 0x89;
        oversizedContent[1] = 0x50;
        oversizedContent[2] = 0x4E;
        oversizedContent[3] = 0x47;
        var imageContent = new ByteArrayContent(oversizedContent);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        uploadContent.Add(imageContent, "Image", "oversized.png");

        var response = await managerClient.PostAsync("/api/center-images/upload", uploadContent);
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>();

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal("Image files must be 5MB or smaller.", payload?.Message);
    }

    [Fact]
    public async Task ManagerUpload_ForUnassignedCenter_ReturnsForbidden()
    {
        using var managerClient = factory.CreateClient();

        var token = await LoginAsync(managerClient, "manager@test.local", "Manager123!Pass");
        managerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var uploadContent = BuildUploadContent(centerId: 2, fileName: "forbidden.png", ValidPngBytes, isPrimary: false);

        var response = await managerClient.PostAsync("/api/center-images/upload", uploadContent);
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<object>>();

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Equal("Managers can only manage images for assigned centers.", payload?.Message);
    }

    [Fact]
    public async Task ManagerCanSetPrimary_AndDeletingPrimaryPromotesRemainingImage()
    {
        using var managerClient = factory.CreateClient();
        using var publicClient = factory.CreateClient();

        var token = await LoginAsync(managerClient, "manager@test.local", "Manager123!Pass");
        managerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var firstImage = await UploadImageAsync(managerClient, centerId: 1, fileName: "primary-one.png", ValidPngBytes, isPrimary: true);
        var secondImage = await UploadImageAsync(managerClient, centerId: 1, fileName: "primary-two.png", ValidPngBytes, isPrimary: false);

        var setPrimaryResponse = await managerClient.PutAsync($"/api/center-images/{secondImage.Id}/set-primary", null);
        Assert.Equal(HttpStatusCode.OK, setPrimaryResponse.StatusCode);

        var galleryAfterPrimaryUpdate = await publicClient.GetFromJsonAsync<ApiEnvelope<List<GalleryCenterImagePayload>>>("/api/centers/1/images");
        Assert.NotNull(galleryAfterPrimaryUpdate?.Data);
        Assert.Contains(galleryAfterPrimaryUpdate.Data!, image => image.Id == secondImage.Id && image.IsPrimary);
        Assert.Contains(galleryAfterPrimaryUpdate.Data!, image => image.Id == firstImage.Id && !image.IsPrimary);

        var deleteResponse = await managerClient.DeleteAsync($"/api/center-images/{secondImage.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var galleryAfterDelete = await publicClient.GetFromJsonAsync<ApiEnvelope<List<GalleryCenterImagePayload>>>("/api/centers/1/images");
        Assert.NotNull(galleryAfterDelete?.Data);
        Assert.DoesNotContain(galleryAfterDelete.Data!, image => image.Id == secondImage.Id);
        Assert.Contains(galleryAfterDelete.Data!, image => image.Id == firstImage.Id && image.IsPrimary);
    }

    [Fact]
    public async Task AdminCanDeleteCenterImage()
    {
        using var managerClient = factory.CreateClient();
        using var adminClient = factory.CreateClient();
        using var publicClient = factory.CreateClient();

        var managerToken = await LoginAsync(managerClient, "manager@test.local", "Manager123!Pass");
        managerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerToken);
        var adminToken = await LoginAsync(adminClient, "admin@test.local", "Admin123!Pass");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminToken);

        var uploadedImage = await UploadImageAsync(managerClient, centerId: 1, fileName: "admin-delete.png", ValidPngBytes, isPrimary: false);

        var deleteResponse = await adminClient.DeleteAsync($"/api/center-images/{uploadedImage.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var galleryResponse = await publicClient.GetFromJsonAsync<ApiEnvelope<List<GalleryCenterImagePayload>>>("/api/centers/1/images");
        Assert.NotNull(galleryResponse?.Data);
        Assert.DoesNotContain(galleryResponse.Data!, image => image.Id == uploadedImage.Id);
    }

    private static MultipartFormDataContent BuildUploadContent(int centerId, string fileName, byte[] fileBytes, bool isPrimary)
    {
        var uploadContent = new MultipartFormDataContent
        {
            { new StringContent(centerId.ToString()), "CenterId" },
            { new StringContent(isPrimary ? "true" : "false"), "IsPrimary" }
        };

        var imageContent = new ByteArrayContent(fileBytes);
        imageContent.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        uploadContent.Add(imageContent, "Image", fileName);
        return uploadContent;
    }

    private static async Task<GalleryCenterImagePayload> UploadImageAsync(HttpClient client, int centerId, string fileName, byte[] fileBytes, bool isPrimary)
    {
        using var uploadContent = BuildUploadContent(centerId, fileName, fileBytes, isPrimary);

        var response = await client.PostAsync("/api/center-images/upload", uploadContent);
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<GalleryCenterImagePayload>>();

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(payload?.Data);
        return payload.Data!;
    }

    private static async Task<string> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<CenterImageLoginPayload>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload?.Data?.Token);
        return payload.Data.Token;
    }
}

public sealed class GalleryCenterImagePayload
{
    public int Id { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public bool IsPrimary { get; set; }
}

public sealed class CenterImageLoginPayload
{
    public string Token { get; set; } = string.Empty;
}
