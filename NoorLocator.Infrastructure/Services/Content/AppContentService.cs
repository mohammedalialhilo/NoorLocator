using Microsoft.EntityFrameworkCore;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Content.Dtos;
using NoorLocator.Application.Content.Interfaces;
using NoorLocator.Infrastructure.Persistence;

namespace NoorLocator.Infrastructure.Services.Content;

public class AppContentService(NoorLocatorDbContext dbContext) : IAppContentService
{
    private const string DefaultLanguageCode = "en";

    public async Task<OperationResult<AboutContentDto>> GetAboutContentAsync(string? languageCode, CancellationToken cancellationToken = default)
    {
        var normalizedLanguageCode = string.IsNullOrWhiteSpace(languageCode)
            ? DefaultLanguageCode
            : languageCode.Trim().ToLowerInvariant();

        var contentEntries = await dbContext.AppContents
            .AsNoTracking()
            .Where(appContent => appContent.LanguageCode == normalizedLanguageCode || appContent.LanguageCode == DefaultLanguageCode)
            .ToArrayAsync(cancellationToken);

        if (contentEntries.Length == 0)
        {
            return OperationResult<AboutContentDto>.Failure("About content is not available.", 404);
        }

        var values = contentEntries
            .GroupBy(appContent => appContent.Key, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(
                group => group.Key,
                group => group
                    .OrderByDescending(appContent => appContent.LanguageCode.Equals(normalizedLanguageCode, StringComparison.OrdinalIgnoreCase))
                    .Select(appContent => appContent.Value)
                    .First(),
                StringComparer.OrdinalIgnoreCase);

        string GetValue(string key) => values.TryGetValue(key, out var value) ? value : string.Empty;

        return OperationResult<AboutContentDto>.Success(new AboutContentDto
        {
            LanguageCode = normalizedLanguageCode,
            SiteTagline = GetValue("site.tagline"),
            Attribution = GetValue("site.attribution"),
            HomeHero = new NarrativeSectionDto
            {
                Title = GetValue("home.hero.title"),
                Description = GetValue("home.hero.description"),
                Highlight = GetValue("home.hero.highlight")
            },
            HomeMission = new NarrativeSectionDto
            {
                Title = GetValue("home.mission.title"),
                Description = GetValue("home.mission.description"),
                Highlight = GetValue("home.mission.highlight")
            },
            HomeFeatures = new FeatureSectionDto
            {
                Title = GetValue("home.features.title"),
                Description = GetValue("home.features.description"),
                Items = BuildFeatureItems(values)
            },
            Vision = new NarrativeSectionDto
            {
                Title = GetValue("about.vision.title"),
                Description = GetValue("about.vision.description"),
                Highlight = GetValue("about.vision.highlight")
            },
            ProblemStatement = new ListSectionDto
            {
                Title = GetValue("about.problem.title"),
                Description = GetValue("about.problem.description"),
                Items = BuildList(values, "about.problem.items")
            },
            Mission = new ListSectionDto
            {
                Title = GetValue("about.mission.title"),
                Description = GetValue("about.mission.description"),
                Items = BuildList(values, "about.mission.items")
            },
            CorePrinciples = new PrincipleSectionDto
            {
                Title = GetValue("about.principles.title"),
                Description = GetValue("about.principles.description"),
                Items = BuildPrinciples(values)
            },
            WhoWeAre = new NarrativeSectionDto
            {
                Title = GetValue("about.identity.title"),
                Description = GetValue("about.identity.description"),
                Highlight = GetValue("about.identity.highlight")
            },
            Closing = new NarrativeSectionDto
            {
                Title = GetValue("about.closing.title"),
                Description = GetValue("about.closing.description"),
                Highlight = GetValue("about.closing.highlight")
            }
        });
    }

    private static IReadOnlyCollection<string> BuildList(IReadOnlyDictionary<string, string> values, string prefix)
    {
        return values
            .Where(pair => pair.Key.StartsWith($"{prefix}.", StringComparison.OrdinalIgnoreCase))
            .OrderBy(pair => pair.Key, StringComparer.OrdinalIgnoreCase)
            .Select(pair => pair.Value)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .ToArray();
    }

    private static IReadOnlyCollection<FeatureHighlightDto> BuildFeatureItems(IReadOnlyDictionary<string, string> values)
    {
        return new[]
        {
            "location",
            "languages",
            "community"
        }
        .Select(key => new FeatureHighlightDto
        {
            Title = values.TryGetValue($"home.features.{key}.title", out var title) ? title : string.Empty,
            Description = values.TryGetValue($"home.features.{key}.description", out var description) ? description : string.Empty
        })
        .Where(feature => !string.IsNullOrWhiteSpace(feature.Title))
        .ToArray();
    }

    private static IReadOnlyCollection<PrincipleDto> BuildPrinciples(IReadOnlyDictionary<string, string> values)
    {
        return new[]
        {
            "trust",
            "community",
            "language",
            "location"
        }
        .Select(key => new PrincipleDto
        {
            Title = values.TryGetValue($"about.principles.{key}.title", out var title) ? title : string.Empty,
            Description = values.TryGetValue($"about.principles.{key}.description", out var description) ? description : string.Empty
        })
        .Where(principle => !string.IsNullOrWhiteSpace(principle.Title))
        .ToArray();
    }
}
