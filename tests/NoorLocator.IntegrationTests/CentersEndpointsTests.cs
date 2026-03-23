using System.Net;
using System.Net.Http.Json;

namespace NoorLocator.IntegrationTests;

public class CentersEndpointsTests(NoorLocatorWebApplicationFactory factory) : IClassFixture<NoorLocatorWebApplicationFactory>
{
    [Fact]
    public async Task Search_ByCity_ReturnsMatchingCenters()
    {
        using var client = factory.CreateClient();

        var response = await client.GetAsync("/api/centers/search?city=Copenhagen");
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<List<CenterSummaryPayload>>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.True(payload!.Success);
        Assert.Single(payload.Data!);
        Assert.Equal("Integration Copenhagen Center", payload.Data![0].Name);
    }

    [Fact]
    public async Task CenterRequests_Create_RequiresAuthentication()
    {
        using var client = factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/center-requests", new
        {
            name = "Unauthorized Center",
            address = "Road 1",
            city = "Malmo",
            country = "Sweden",
            latitude = 55.605m,
            longitude = 13.0038m,
            description = "Should fail."
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}

public sealed class CenterSummaryPayload
{
    public string Name { get; set; } = string.Empty;
}
