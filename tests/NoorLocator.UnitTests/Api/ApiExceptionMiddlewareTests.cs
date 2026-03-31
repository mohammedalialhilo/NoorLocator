using System.Text.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging.Abstractions;
using NoorLocator.Api.Middleware;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.UnitTests.Api;

public class ApiExceptionMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_InProduction_ReturnsGenericMessageWithoutExceptionDetails()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ApiExceptionMiddleware(
            _ => throw new InvalidOperationException("Sensitive production failure."),
            NullLogger<ApiExceptionMiddleware>.Instance,
            CreateEnvironment(Environments.Production));

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var payload = await JsonSerializer.DeserializeAsync<ApiResponse<ApiErrorDetails>>(
            context.Response.Body,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(payload!.Success);
        Assert.Equal("An unexpected error occurred while processing the request.", payload.Message);
        Assert.NotNull(payload.Data);
        Assert.False(string.IsNullOrWhiteSpace(payload.Data!.TraceId));
        Assert.Empty(payload.Data.Errors);
    }

    [Fact]
    public async Task InvokeAsync_InDevelopment_ReturnsDetailedExceptionInformation()
    {
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        var middleware = new ApiExceptionMiddleware(
            _ => throw new InvalidOperationException("Sensitive development failure."),
            NullLogger<ApiExceptionMiddleware>.Instance,
            CreateEnvironment(Environments.Development));

        await middleware.InvokeAsync(context);

        context.Response.Body.Position = 0;
        var payload = await JsonSerializer.DeserializeAsync<ApiResponse<ApiErrorDetails>>(
            context.Response.Body,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        Assert.NotNull(payload);
        Assert.False(payload!.Success);
        Assert.Equal("Sensitive development failure.", payload.Message);
        Assert.NotNull(payload.Data);
        Assert.Contains("InvalidOperationException", payload.Data!.Errors);
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
