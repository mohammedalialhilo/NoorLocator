using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using NoorLocator.Api.Controllers;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.UnitTests.Api;

public class HealthControllerTests
{
    [Theory]
    [InlineData("Production")]
    [InlineData("Staging")]
    public void Get_OutsideDevelopmentAndTesting_DoesNotExposeEnvironment(string environmentName)
    {
        var controller = new HealthController(CreateEnvironment(environmentName));

        var result = controller.Get();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ApiResponse<object>>(okResult.Value);
        var data = Assert.IsAssignableFrom<IDictionary<string, object?>>(payload.Data);

        Assert.Equal("Healthy", data["status"]);
        Assert.False(data.ContainsKey("environment"));
    }

    [Theory]
    [InlineData("Development")]
    [InlineData("Testing")]
    public void Get_InDevelopmentOrTesting_ExposesEnvironment(string environmentName)
    {
        var controller = new HealthController(CreateEnvironment(environmentName));

        var result = controller.Get();

        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var payload = Assert.IsType<ApiResponse<object>>(okResult.Value);
        var data = Assert.IsAssignableFrom<IDictionary<string, object?>>(payload.Data);

        Assert.Equal(environmentName, data["environment"]);
    }

    private static TestWebHostEnvironment CreateEnvironment(string environmentName)
    {
        return new TestWebHostEnvironment
        {
            EnvironmentName = environmentName,
            ApplicationName = "NoorLocator",
            ContentRootPath = Directory.GetCurrentDirectory(),
            WebRootPath = Directory.GetCurrentDirectory()
        };
    }

    private sealed class TestWebHostEnvironment : IWebHostEnvironment
    {
        public string EnvironmentName { get; set; } = Environments.Production;

        public string ApplicationName { get; set; } = string.Empty;

        public string ContentRootPath { get; set; } = string.Empty;

        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();

        public string WebRootPath { get; set; } = string.Empty;

        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
