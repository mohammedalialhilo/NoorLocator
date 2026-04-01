using Microsoft.EntityFrameworkCore;
using NoorLocator.Application.Centers.Dtos;
using NoorLocator.Application.Centers.Interfaces;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Languages.Dtos;
using NoorLocator.Application.Majalis.Dtos;
using NoorLocator.Infrastructure.Persistence;

namespace NoorLocator.Infrastructure.Services.Centers;

public class CenterService(NoorLocatorDbContext dbContext) : ICenterService
{
    public async Task<OperationResult<IReadOnlyCollection<CenterSummaryDto>>> GetCentersAsync(CenterLocationQueryDto query, CancellationToken cancellationToken = default)
    {
        var centers = await dbContext.Centers
            .AsNoTracking()
            .OrderBy(center => center.Country)
            .ThenBy(center => center.City)
            .ThenBy(center => center.Name)
            .Select(center => new CenterSummaryDto
            {
                Id = center.Id,
                Name = center.Name,
                Description = center.Description,
                Address = center.Address,
                City = center.City,
                Country = center.Country,
                Latitude = center.Latitude,
                Longitude = center.Longitude,
                Languages = center.CenterLanguages
                    .Where(centerLanguage => centerLanguage.Language != null)
                    .Select(centerLanguage => new LanguageDto
                    {
                        Id = centerLanguage.Language!.Id,
                        Name = centerLanguage.Language.Name,
                        Code = centerLanguage.Language.Code
                    })
                    .OrderBy(language => language.Name)
                    .ToArray()
            })
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<CenterSummaryDto>>.Success(
            ApplyDistance(centers, query.Lat, query.Lng));
    }

    public async Task<OperationResult<CenterDetailsDto>> GetCenterByIdAsync(int id, CenterLocationQueryDto query, CancellationToken cancellationToken = default)
    {
        var todayUtc = DateTime.UtcNow.Date;

        var center = await dbContext.Centers
            .AsNoTracking()
            .AsSplitQuery()
            .Include(currentCenter => currentCenter.CenterLanguages)
                .ThenInclude(centerLanguage => centerLanguage.Language)
            .Include(currentCenter => currentCenter.Majalis)
                .ThenInclude(majlis => majlis.MajlisLanguages)
                    .ThenInclude(majlisLanguage => majlisLanguage.Language)
            .SingleOrDefaultAsync(currentCenter => currentCenter.Id == id, cancellationToken);

        if (center is null)
        {
            return OperationResult<CenterDetailsDto>.Failure("Center not found.", 404);
        }

        var dto = new CenterDetailsDto
        {
            Id = center.Id,
            Name = center.Name,
            Description = center.Description,
            Address = center.Address,
            City = center.City,
            Country = center.Country,
            Latitude = center.Latitude,
            Longitude = center.Longitude,
            DistanceKm = query.Lat.HasValue && query.Lng.HasValue
                ? CalculateApproximateDistanceKm((double)query.Lat.Value, (double)query.Lng.Value, (double)center.Latitude, (double)center.Longitude)
                : null,
            Languages = center.CenterLanguages
                .Where(centerLanguage => centerLanguage.Language is not null)
                .Select(centerLanguage => new LanguageDto
                {
                    Id = centerLanguage.Language!.Id,
                    Name = centerLanguage.Language.Name,
                    Code = centerLanguage.Language.Code
                })
                .OrderBy(language => language.Name)
                .ToArray(),
            Majalis = center.Majalis
                .Where(majlis => majlis.Date >= todayUtc)
                .OrderBy(majlis => majlis.Date)
                .Select(MapMajlis)
                .ToArray()
        };

        return OperationResult<CenterDetailsDto>.Success(dto);
    }

    public async Task<OperationResult<IReadOnlyCollection<LanguageDto>>> GetCenterLanguagesAsync(int centerId, CancellationToken cancellationToken = default)
    {
        if (!await CenterExistsAsync(centerId, cancellationToken))
        {
            return OperationResult<IReadOnlyCollection<LanguageDto>>.Failure("Center not found.", 404);
        }

        var languages = await dbContext.CenterLanguages
            .AsNoTracking()
            .Where(centerLanguage => centerLanguage.CenterId == centerId)
            .Where(centerLanguage => centerLanguage.Language != null)
            .OrderBy(centerLanguage => centerLanguage.Language!.Name)
            .Select(centerLanguage => new LanguageDto
            {
                Id = centerLanguage.Language!.Id,
                Name = centerLanguage.Language.Name,
                Code = centerLanguage.Language.Code
            })
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<LanguageDto>>.Success(languages);
    }

    public async Task<OperationResult<IReadOnlyCollection<MajlisDto>>> GetCenterMajalisAsync(int centerId, CancellationToken cancellationToken = default)
    {
        if (!await CenterExistsAsync(centerId, cancellationToken))
        {
            return OperationResult<IReadOnlyCollection<MajlisDto>>.Failure("Center not found.", 404);
        }

        var todayUtc = DateTime.UtcNow.Date;

        var majalis = await dbContext.Majalis
            .AsNoTracking()
            .Where(majlis => majlis.CenterId == centerId && majlis.Date >= todayUtc)
            .Include(majlis => majlis.MajlisLanguages)
                .ThenInclude(majlisLanguage => majlisLanguage.Language)
            .OrderBy(majlis => majlis.Date)
            .Select(majlis => new MajlisDto
            {
                Id = majlis.Id,
                Title = majlis.Title,
                Description = majlis.Description,
                ImageUrl = majlis.ImageUrl,
                Date = majlis.Date,
                Time = majlis.Time,
                CenterId = majlis.CenterId,
                CenterName = majlis.Center != null ? majlis.Center.Name : string.Empty,
                CenterCity = majlis.Center != null ? majlis.Center.City : string.Empty,
                CenterCountry = majlis.Center != null ? majlis.Center.Country : string.Empty,
                Languages = majlis.MajlisLanguages
                    .Where(majlisLanguage => majlisLanguage.Language != null)
                    .Select(majlisLanguage => new LanguageDto
                    {
                        Id = majlisLanguage.Language!.Id,
                        Name = majlisLanguage.Language.Name,
                        Code = majlisLanguage.Language.Code
                    })
                    .ToArray()
            })
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<MajlisDto>>.Success(majalis);
    }

    public async Task<OperationResult<IReadOnlyCollection<CenterSummaryDto>>> GetNearestCentersAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
    {
        var centers = await dbContext.Centers
            .AsNoTracking()
            .Select(center => new CenterSummaryDto
            {
                Id = center.Id,
                Name = center.Name,
                Description = center.Description,
                Address = center.Address,
                City = center.City,
                Country = center.Country,
                Latitude = center.Latitude,
                Longitude = center.Longitude,
                Languages = center.CenterLanguages
                    .Where(centerLanguage => centerLanguage.Language != null)
                    .Select(centerLanguage => new LanguageDto
                    {
                        Id = centerLanguage.Language!.Id,
                        Name = centerLanguage.Language.Name,
                        Code = centerLanguage.Language.Code
                    })
                    .OrderBy(language => language.Name)
                    .ToArray()
            })
            .ToArrayAsync(cancellationToken);

        var sorted = ApplyDistance(centers, latitude, longitude, sortByDistance: true);
        return OperationResult<IReadOnlyCollection<CenterSummaryDto>>.Success(sorted);
    }

    public async Task<OperationResult<IReadOnlyCollection<CenterSummaryDto>>> SearchCentersAsync(CenterSearchQueryDto query, CancellationToken cancellationToken = default)
    {
        var searchQuery = dbContext.Centers
            .AsNoTracking()
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Query))
        {
            var normalizedQuery = query.Query.Trim();
            searchQuery = searchQuery.Where(center =>
                center.Name.Contains(normalizedQuery) ||
                center.Description.Contains(normalizedQuery) ||
                center.Address.Contains(normalizedQuery) ||
                center.City.Contains(normalizedQuery) ||
                center.Country.Contains(normalizedQuery));
        }

        if (!string.IsNullOrWhiteSpace(query.City))
        {
            var city = query.City.Trim();
            searchQuery = searchQuery.Where(center => center.City.Contains(city));
        }

        if (!string.IsNullOrWhiteSpace(query.Country))
        {
            var country = query.Country.Trim();
            searchQuery = searchQuery.Where(center => center.Country.Contains(country));
        }

        if (!string.IsNullOrWhiteSpace(query.LanguageCode))
        {
            var languageCode = query.LanguageCode.Trim().ToLowerInvariant();
            searchQuery = searchQuery.Where(center =>
                center.CenterLanguages.Any(centerLanguage => centerLanguage.Language != null && centerLanguage.Language.Code == languageCode));
        }

        var centers = await searchQuery
            .OrderBy(center => center.Country)
            .ThenBy(center => center.City)
            .ThenBy(center => center.Name)
            .Select(center => new CenterSummaryDto
            {
                Id = center.Id,
                Name = center.Name,
                Description = center.Description,
                Address = center.Address,
                City = center.City,
                Country = center.Country,
                Latitude = center.Latitude,
                Longitude = center.Longitude,
                Languages = center.CenterLanguages
                    .Where(centerLanguage => centerLanguage.Language != null)
                    .Select(centerLanguage => new LanguageDto
                    {
                        Id = centerLanguage.Language!.Id,
                        Name = centerLanguage.Language.Name,
                        Code = centerLanguage.Language.Code
                    })
                    .OrderBy(language => language.Name)
                    .ToArray()
            })
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<CenterSummaryDto>>.Success(
            ApplyDistance(centers, query.Lat, query.Lng, sortByDistance: query.Lat.HasValue && query.Lng.HasValue));
    }

    private async Task<bool> CenterExistsAsync(int centerId, CancellationToken cancellationToken)
        => await dbContext.Centers.AnyAsync(center => center.Id == centerId, cancellationToken);

    private static IReadOnlyCollection<CenterSummaryDto> ApplyDistance(
        IReadOnlyCollection<CenterSummaryDto> centers,
        decimal? latitude,
        decimal? longitude,
        bool sortByDistance = false)
    {
        if (!latitude.HasValue || !longitude.HasValue)
        {
            return centers.ToArray();
        }

        var withDistance = centers
            .Select(center =>
            {
                center.DistanceKm = CalculateApproximateDistanceKm(
                    (double)latitude.Value,
                    (double)longitude.Value,
                    (double)center.Latitude,
                    (double)center.Longitude);
                return center;
            })
            .ToArray();

        if (!sortByDistance)
        {
            return withDistance;
        }

        return withDistance
            .OrderBy(center => center.DistanceKm)
            .ThenBy(center => center.Name)
            .ToArray();
    }

    private static MajlisDto MapMajlis(Domain.Entities.Majlis majlis)
    {
        return new MajlisDto
        {
            Id = majlis.Id,
            Title = majlis.Title,
            Description = majlis.Description,
            ImageUrl = majlis.ImageUrl,
            Date = majlis.Date,
            Time = majlis.Time,
            CenterId = majlis.CenterId,
            CenterName = majlis.Center?.Name ?? string.Empty,
            CenterCity = majlis.Center?.City ?? string.Empty,
            CenterCountry = majlis.Center?.Country ?? string.Empty,
            Languages = majlis.MajlisLanguages
                .Where(majlisLanguage => majlisLanguage.Language is not null)
                .Select(majlisLanguage => new LanguageDto
                {
                    Id = majlisLanguage.Language!.Id,
                    Name = majlisLanguage.Language.Name,
                    Code = majlisLanguage.Language.Code
                })
                .OrderBy(language => language.Name)
                .ToArray()
        };
    }

    private static double CalculateApproximateDistanceKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusKm = 6371d;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLng = DegreesToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return Math.Round(earthRadiusKm * c, 1, MidpointRounding.AwayFromZero);
    }

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180d);
}
