using System.Net;
using System.Net.Http.Json;

namespace NoorLocator.IntegrationTests;

public class ContentEndpointsTests(NoorLocatorWebApplicationFactory factory) : IClassFixture<NoorLocatorWebApplicationFactory>
{
    [Fact]
    public async Task AboutContentEndpoint_ReturnsManifestoBackedContent()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/content/about");
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<AboutContentPayload>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload?.Data);
        Assert.Equal("Vision", payload.Data!.Vision.Title);
        Assert.Equal("Connecting you to Shia centers and majalis worldwide", payload.Data.SiteTagline);
        Assert.Equal(3, payload.Data.HomeFeatures.Items.Count);
    }

    [Fact]
    public async Task AboutFrontendRoute_ReturnsAboutPageMarkup()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/about");
        var html = await response.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("About NoorLocator", html);
    }
}

public sealed class AboutContentPayload
{
    public string SiteTagline { get; set; } = string.Empty;

    public AboutNarrativePayload Vision { get; set; } = new();

    public AboutFeatureSectionPayload HomeFeatures { get; set; } = new();
}

public sealed class AboutNarrativePayload
{
    public string Title { get; set; } = string.Empty;
}

public sealed class AboutFeatureSectionPayload
{
    public List<AboutFeaturePayload> Items { get; set; } = [];
}

public sealed class AboutFeaturePayload
{
    public string Title { get; set; } = string.Empty;
}
