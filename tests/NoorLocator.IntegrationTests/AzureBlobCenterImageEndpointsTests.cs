using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace NoorLocator.IntegrationTests;

public class AzureBlobCenterImageEndpointsTests(NoorLocatorWebApplicationFactory factory) : IClassFixture<NoorLocatorWebApplicationFactory>
{
    private static readonly byte[] ValidPngBytes = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO5+9FoAAAAASUVORK5CYII=");

    [Fact]
    public async Task ManagerUpload_WithAzureBlobStorage_SavesPublicBlobUrl_AndDeleteRemovesBlob()
    {
        await using var fakeBlobServer = new FakeBlobStorageServer();
        using var configuredFactory = CreateAzureBlobFactory(factory, fakeBlobServer);
        using var managerClient = configuredFactory.CreateClient();
        using var publicClient = configuredFactory.CreateClient();
        using var blobClient = new HttpClient();

        var token = await LoginAsync(managerClient, "manager@test.local", "Manager123!Pass");
        managerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var uploadPayload = await UploadImageAsync(managerClient, centerId: 1, fileName: "azure-blob.png", ValidPngBytes, isPrimary: true);

        Assert.StartsWith(fakeBlobServer.GetContainerBaseUrl("uploads"), uploadPayload.ImageUrl, StringComparison.OrdinalIgnoreCase);
        Assert.True(fakeBlobServer.PublicBlobAccessWasRequested);
        Assert.Equal(1, fakeBlobServer.UploadRequestCount);

        var imageResponse = await blobClient.GetAsync(uploadPayload.ImageUrl);
        var imageBytes = await imageResponse.Content.ReadAsByteArrayAsync();

        Assert.Equal(HttpStatusCode.OK, imageResponse.StatusCode);
        Assert.Equal("image/png", imageResponse.Content.Headers.ContentType?.MediaType);
        Assert.Equal(ValidPngBytes, imageBytes);

        var galleryResponse = await publicClient.GetAsync("/api/centers/1/images");
        var galleryEnvelope = await galleryResponse.Content.ReadFromJsonAsync<ApiEnvelope<List<GalleryCenterImagePayload>>>();

        Assert.Equal(HttpStatusCode.OK, galleryResponse.StatusCode);
        Assert.NotNull(galleryEnvelope?.Data);
        Assert.Contains(galleryEnvelope.Data!, image => image.Id == uploadPayload.Id && image.ImageUrl == uploadPayload.ImageUrl);

        var deleteResponse = await managerClient.DeleteAsync($"/api/center-images/{uploadPayload.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);
        Assert.Equal(1, fakeBlobServer.DeleteRequestCount);

        var deletedImageResponse = await blobClient.GetAsync(uploadPayload.ImageUrl);
        Assert.Equal(HttpStatusCode.NotFound, deletedImageResponse.StatusCode);
    }

    [Fact]
    public async Task ManagerUpload_InvalidFileType_WithAzureBlobStorage_ReturnsBadRequest_WithoutBlobWrite()
    {
        await using var fakeBlobServer = new FakeBlobStorageServer();
        using var configuredFactory = CreateAzureBlobFactory(factory, fakeBlobServer);
        using var managerClient = configuredFactory.CreateClient();

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
        Assert.Equal(0, fakeBlobServer.UploadRequestCount);
        Assert.Equal(0, fakeBlobServer.ContainerCreateRequestCount);
    }

    private static WebApplicationFactory<Program> CreateAzureBlobFactory(NoorLocatorWebApplicationFactory factory, FakeBlobStorageServer fakeBlobServer)
    {
        return factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureAppConfiguration((_, configurationBuilder) =>
            {
                configurationBuilder.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MediaStorage:Provider"] = "AzureBlob",
                    ["AzureBlobStorage:ConnectionString"] = fakeBlobServer.ConnectionString,
                    ["AzureBlobStorage:ContainerName"] = "uploads",
                    ["AzureBlobStorage:CreateContainerIfMissing"] = "true",
                    ["AzureBlobStorage:UseBlobPublicAccess"] = "true"
                });
            });
        });
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
