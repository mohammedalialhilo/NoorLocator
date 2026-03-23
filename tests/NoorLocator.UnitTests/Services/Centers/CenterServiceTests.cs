using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Domain.Entities;
using NoorLocator.Infrastructure.Services.Centers;
using NoorLocator.UnitTests.TestHelpers;

namespace NoorLocator.UnitTests.Services.Centers;

public class CenterServiceTests
{
    [Fact]
    public async Task GetNearestCentersAsync_SortsByDistanceAndPopulatesDistance()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();
        dbContext.Centers.AddRange(
            new Center
            {
                Name = "Copenhagen Center",
                Address = "Street 1",
                City = "Copenhagen",
                Country = "Denmark",
                Latitude = 55.6761m,
                Longitude = 12.5683m,
                Description = "Near the caller."
            },
            new Center
            {
                Name = "Stockholm Center",
                Address = "Street 2",
                City = "Stockholm",
                Country = "Sweden",
                Latitude = 59.3293m,
                Longitude = 18.0686m,
                Description = "Farther away."
            });
        await dbContext.SaveChangesAsync();

        var service = new CenterService(dbContext);

        var result = await service.GetNearestCentersAsync(55.6761m, 12.5683m);

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        Assert.Equal(2, result.Data.Count);
        Assert.Equal("Copenhagen Center", result.Data.First().Name);
        Assert.NotNull(result.Data.First().DistanceKm);
        Assert.True(result.Data.First().DistanceKm <= result.Data.Last().DistanceKm);
    }

    [Fact]
    public async Task SearchCentersAsync_FiltersByLanguageAndCity()
    {
        await using var dbContext = TestDbContextFactory.CreateContext();

        var arabic = new Language { Id = 1, Name = "Arabic", Code = "ar" };
        var english = new Language { Id = 2, Name = "English", Code = "en" };
        var copenhagenCenter = new Center
        {
            Id = 1,
            Name = "Imam Ali Center",
            Address = "Street 1",
            City = "Copenhagen",
            Country = "Denmark",
            Latitude = 55.6761m,
            Longitude = 12.5683m,
            Description = "Arabic and English."
        };
        var stockholmCenter = new Center
        {
            Id = 2,
            Name = "Stockholm House",
            Address = "Street 2",
            City = "Stockholm",
            Country = "Sweden",
            Latitude = 59.3293m,
            Longitude = 18.0686m,
            Description = "English only."
        };

        dbContext.Languages.AddRange(arabic, english);
        dbContext.Centers.AddRange(copenhagenCenter, stockholmCenter);
        dbContext.CenterLanguages.AddRange(
            new CenterLanguage { CenterId = 1, LanguageId = 1, Language = arabic, Center = copenhagenCenter },
            new CenterLanguage { CenterId = 1, LanguageId = 2, Language = english, Center = copenhagenCenter },
            new CenterLanguage { CenterId = 2, LanguageId = 2, Language = english, Center = stockholmCenter });
        await dbContext.SaveChangesAsync();

        var service = new CenterService(dbContext);

        var result = await service.SearchCentersAsync(new CenterSearchQueryDto
        {
            City = "Copenhagen",
            LanguageCode = "ar"
        });

        Assert.True(result.Succeeded);
        Assert.NotNull(result.Data);
        var matches = result.Data.ToArray();
        Assert.Single(matches);
        Assert.Equal("Imam Ali Center", matches[0].Name);
    }
}
