using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.FileProviders;
using NoorLocator.Application.Common.Configuration;
using NoorLocator.Application.Common.Models;
using NoorLocator.Infrastructure.Services.Media;

namespace NoorLocator.UnitTests.Services.Media;

public class LocalMediaStorageServiceTests : IDisposable
{
    private readonly string tempRootPath = Path.Combine(Path.GetTempPath(), "NoorLocatorUnitTests", Guid.NewGuid().ToString("N"));

    [Fact]
    public async Task SaveImageAsync_RejectsInvalidImageSignature()
    {
        Directory.CreateDirectory(tempRootPath);
        var service = CreateService();

        var result = await service.SaveImageAsync(new UploadFilePayload
        {
            FileName = "not-an-image.png",
            ContentType = "image/png",
            Content = [0x00, 0x01, 0x02, 0x03]
        }, "center-images");

        Assert.False(result.Succeeded);
        Assert.Equal(400, result.StatusCode);
        Assert.Equal("The uploaded file content is not a supported image.", result.Message);
    }

    [Fact]
    public async Task SaveImageAsync_StoresValidPngAndDeleteFileAsync_RemovesIt()
    {
        Directory.CreateDirectory(tempRootPath);
        var service = CreateService();

        var result = await service.SaveImageAsync(new UploadFilePayload
        {
            FileName = "center.png",
            ContentType = "image/png",
            Content = Convert.FromBase64String("iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAQAAAC1HAwCAAAAC0lEQVR42mP8/x8AAwMCAO5+9FoAAAAASUVORK5CYII=")
        }, "center-images");

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);

        var relativeFilePath = result.Data!.PublicUrl
            .Replace("/uploads/", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Replace('/', Path.DirectorySeparatorChar);
        var fullFilePath = Path.Combine(tempRootPath, relativeFilePath);

        Assert.True(File.Exists(fullFilePath));

        await service.DeleteFileAsync(result.Data.PublicUrl);

        Assert.False(File.Exists(fullFilePath));
    }

    public void Dispose()
    {
        if (Directory.Exists(tempRootPath))
        {
            Directory.Delete(tempRootPath, recursive: true);
        }
    }

    private LocalMediaStorageService CreateService()
    {
        return new LocalMediaStorageService(
            new TestHostEnvironment
            {
                ApplicationName = "NoorLocator.UnitTests",
                ContentRootPath = tempRootPath,
                ContentRootFileProvider = new PhysicalFileProvider(tempRootPath),
                EnvironmentName = "Testing"
            },
            Options.Create(new MediaStorageSettings
            {
                PublicBasePath = "/uploads",
                RelativeRootPath = tempRootPath,
                MaxImageSizeBytes = 5 * 1024 * 1024
            }));
    }

    private sealed class TestHostEnvironment : IHostEnvironment
    {
        public string EnvironmentName { get; set; } = "Testing";

        public string ApplicationName { get; set; } = string.Empty;

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
    }
}
