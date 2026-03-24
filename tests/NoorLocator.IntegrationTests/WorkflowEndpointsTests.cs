using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace NoorLocator.IntegrationTests;

public class WorkflowEndpointsTests
{
    [Fact]
    public async Task UserContributionWorkflow_SubmitsPendingItemsVisibleToAdmin()
    {
        using var factory = new NoorLocatorWebApplicationFactory();
        using var userClient = factory.CreateClient();
        using var adminClient = factory.CreateClient();

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var centerName = $"Integration Pending Center {uniqueSuffix}";
        var suggestionMessage = $"Integration suggestion {uniqueSuffix}";

        var userAuth = await LoginAsync(userClient, "user@test.local", "User123!Pass");
        userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.Token);

        var centerRequestResponse = await userClient.PostAsJsonAsync("/api/center-requests", new
        {
            name = centerName,
            address = "Pending Road 1",
            city = "Malmo",
            country = "Sweden",
            latitude = 55.605m,
            longitude = 13.0038m,
            description = "Pending center submission."
        });
        Assert.Equal(HttpStatusCode.Accepted, centerRequestResponse.StatusCode);

        var suggestionResponse = await userClient.PostAsJsonAsync("/api/suggestions", new
        {
            message = suggestionMessage,
            type = "Feature"
        });
        Assert.Equal(HttpStatusCode.Accepted, suggestionResponse.StatusCode);

        var languageSuggestionResponse = await userClient.PostAsJsonAsync("/api/center-language-suggestions", new
        {
            centerId = 2,
            languageId = 1
        });
        Assert.Equal(HttpStatusCode.Accepted, languageSuggestionResponse.StatusCode);

        var managerRequestResponse = await userClient.PostAsJsonAsync("/api/manager/request", new
        {
            centerId = 1
        });
        Assert.Equal(HttpStatusCode.Accepted, managerRequestResponse.StatusCode);

        var adminAuth = await LoginAsync(adminClient, "admin@test.local", "Admin123!Pass");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.Token);

        var centerQueue = await ReadEnvelopeAsync<List<AdminCenterRequestTestDto>>(adminClient, "/api/admin/center-requests");
        Assert.Contains(centerQueue, request => request.Name == centerName && request.Status == "Pending");

        var managerQueue = await ReadEnvelopeAsync<List<AdminManagerRequestTestDto>>(adminClient, "/api/admin/manager-requests");
        Assert.Contains(managerQueue, request => request.UserEmail == "user@test.local" && request.CenterId == 1 && request.Status == "Pending");

        var languageQueue = await ReadEnvelopeAsync<List<AdminCenterLanguageSuggestionTestDto>>(adminClient, "/api/admin/center-language-suggestions");
        Assert.Contains(languageQueue, request => request.CenterId == 2 && request.LanguageId == 1 && request.Status == "Pending");

        var suggestionQueue = await ReadEnvelopeAsync<List<AdminSuggestionTestDto>>(adminClient, "/api/admin/suggestions");
        Assert.Contains(suggestionQueue, request => request.Message == suggestionMessage && request.Status == "Pending");
    }

    [Fact]
    public async Task ManagerMajlisCrud_WorksForAssignedCenter_AndBlocksUnassignedCenter()
    {
        using var factory = new NoorLocatorWebApplicationFactory();
        using var managerClient = factory.CreateClient();

        var managerAuth = await LoginAsync(managerClient, "manager@test.local", "Manager123!Pass");
        managerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", managerAuth.Token);

        var title = $"Integration Majlis {Guid.NewGuid():N}"[..26];
        var createResponse = await managerClient.PostAsJsonAsync("/api/majalis", new
        {
            title,
            description = "Majlis lifecycle test.",
            date = DateTime.UtcNow.Date.AddDays(10),
            time = "20:15",
            centerId = 1,
            languageIds = new[] { 1, 2 }
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var majalisAfterCreate = await ReadEnvelopeAsync<List<MajlisTestDto>>(managerClient, "/api/majalis?centerId=1");
        var createdMajlis = Assert.Single(majalisAfterCreate, majlis => majlis.Title == title);
        Assert.Equal(2, createdMajlis.Languages.Count);

        var updateResponse = await managerClient.PutAsJsonAsync($"/api/majalis/{createdMajlis.Id}", new
        {
            title = $"{title} Updated",
            description = "Updated majlis lifecycle test.",
            date = DateTime.UtcNow.Date.AddDays(11),
            time = "21:00",
            centerId = 1,
            languageIds = new[] { 2 }
        });
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updatedMajlis = await ReadEnvelopeAsync<MajlisTestDto>(managerClient, $"/api/majalis/{createdMajlis.Id}");
        Assert.Equal($"{title} Updated", updatedMajlis.Title);
        Assert.Single(updatedMajlis.Languages);
        Assert.Equal("English", updatedMajlis.Languages[0].Name);

        var forbiddenCreateResponse = await managerClient.PostAsJsonAsync("/api/majalis", new
        {
            title = "Forbidden Center Majlis",
            description = "Should not be allowed.",
            date = DateTime.UtcNow.Date.AddDays(12),
            time = "18:00",
            centerId = 2,
            languageIds = new[] { 2 }
        });
        Assert.Equal(HttpStatusCode.Forbidden, forbiddenCreateResponse.StatusCode);

        var deleteResponse = await managerClient.DeleteAsync($"/api/majalis/{createdMajlis.Id}");
        Assert.Equal(HttpStatusCode.OK, deleteResponse.StatusCode);

        var deletedResponse = await managerClient.GetAsync($"/api/majalis/{createdMajlis.Id}");
        Assert.Equal(HttpStatusCode.NotFound, deletedResponse.StatusCode);
    }

    [Fact]
    public async Task AdminApprovalWorkflow_PublishesCenter_AssignsManager_AndReviewsSuggestion()
    {
        using var factory = new NoorLocatorWebApplicationFactory();
        using var userClient = factory.CreateClient();
        using var adminClient = factory.CreateClient();
        using var managerClient = factory.CreateClient();

        var uniqueSuffix = Guid.NewGuid().ToString("N")[..8];
        var centerName = $"Approved Center {uniqueSuffix}";
        var suggestionMessage = $"Review suggestion {uniqueSuffix}";

        var userAuth = await LoginAsync(userClient, "user@test.local", "User123!Pass");
        userClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", userAuth.Token);

        await userClient.PostAsJsonAsync("/api/center-requests", new
        {
            name = centerName,
            address = "Approval Street 9",
            city = "Gothenburg",
            country = "Sweden",
            latitude = 57.7089m,
            longitude = 11.9746m,
            description = "Admin approval workflow test."
        });
        await userClient.PostAsJsonAsync("/api/manager/request", new { centerId = 2 });
        await userClient.PostAsJsonAsync("/api/center-language-suggestions", new { centerId = 1, languageId = 3 });
        await userClient.PostAsJsonAsync("/api/suggestions", new { message = suggestionMessage, type = "Feature" });

        var adminAuth = await LoginAsync(adminClient, "admin@test.local", "Admin123!Pass");
        adminClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", adminAuth.Token);

        var centerRequest = (await ReadEnvelopeAsync<List<AdminCenterRequestTestDto>>(adminClient, "/api/admin/center-requests"))
            .Single(request => request.Name == centerName);
        var managerRequest = (await ReadEnvelopeAsync<List<AdminManagerRequestTestDto>>(adminClient, "/api/admin/manager-requests"))
            .Single(request => request.UserEmail == "user@test.local" && request.CenterId == 2 && request.Status == "Pending");
        var languageSuggestion = (await ReadEnvelopeAsync<List<AdminCenterLanguageSuggestionTestDto>>(adminClient, "/api/admin/center-language-suggestions"))
            .Single(request => request.CenterId == 1 && request.LanguageId == 3 && request.Status == "Pending");
        var suggestion = (await ReadEnvelopeAsync<List<AdminSuggestionTestDto>>(adminClient, "/api/admin/suggestions"))
            .Single(request => request.Message == suggestionMessage && request.Status == "Pending");

        Assert.Equal(HttpStatusCode.OK, (await adminClient.PostAsync($"/api/admin/center-requests/{centerRequest.Id}/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await adminClient.PostAsync($"/api/admin/manager-requests/{managerRequest.Id}/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await adminClient.PostAsync($"/api/admin/center-language-suggestions/{languageSuggestion.Id}/approve", null)).StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await adminClient.PutAsync($"/api/admin/suggestions/{suggestion.Id}/review", new StringContent(string.Empty))).StatusCode);

        var adminCenters = await ReadEnvelopeAsync<List<AdminCenterSummaryTestDto>>(adminClient, "/api/admin/centers");
        Assert.Contains(adminCenters, center => center.Name == centerName);

        var publicCenterLanguages = await ReadEnvelopeAsync<List<LanguageTestDto>>(adminClient, "/api/centers/1/languages");
        Assert.Contains(publicCenterLanguages, language => language.Id == 3 && language.Code == "sv");

        var reviewedSuggestions = await ReadEnvelopeAsync<List<AdminSuggestionTestDto>>(adminClient, "/api/admin/suggestions");
        Assert.Contains(reviewedSuggestions, item => item.Id == suggestion.Id && item.Status == "Reviewed");

        var refreshedManagerAuth = await LoginAsync(managerClient, "user@test.local", "User123!Pass");
        managerClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", refreshedManagerAuth.Token);

        var managedCenters = await ReadEnvelopeAsync<List<ManagedCenterTestDto>>(managerClient, "/api/manager/my-centers");
        Assert.Contains(managedCenters, center => center.Id == 2);
    }

    private static async Task<AuthPayload> LoginAsync(HttpClient client, string email, string password)
    {
        var response = await client.PostAsJsonAsync("/api/auth/login", new { email, password });
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<AuthPayload>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload?.Data);
        return payload!.Data!;
    }

    private static async Task<T> ReadEnvelopeAsync<T>(HttpClient client, string path)
    {
        var response = await client.GetAsync(path);
        var payload = await response.Content.ReadFromJsonAsync<ApiEnvelope<T>>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(payload);
        Assert.NotNull(payload!.Data);
        return payload.Data!;
    }
}

public sealed class AdminCenterRequestTestDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}

public sealed class AdminManagerRequestTestDto
{
    public int Id { get; set; }

    public int CenterId { get; set; }

    public string UserEmail { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}

public sealed class AdminCenterLanguageSuggestionTestDto
{
    public int Id { get; set; }

    public int CenterId { get; set; }

    public int LanguageId { get; set; }

    public string Status { get; set; } = string.Empty;
}

public sealed class AdminSuggestionTestDto
{
    public int Id { get; set; }

    public string Message { get; set; } = string.Empty;

    public string Status { get; set; } = string.Empty;
}

public sealed class MajlisTestDto
{
    public int Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public List<LanguageTestDto> Languages { get; set; } = [];
}

public sealed class LanguageTestDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;
}

public sealed class AdminCenterSummaryTestDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;
}

public sealed class ManagedCenterTestDto
{
    public int Id { get; set; }
}
