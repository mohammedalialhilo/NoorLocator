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
    public async Task<OperationResult<CenterDetailsDto>> GetCenterByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var center = await dbContext.Centers
            .AsNoTracking()
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
            Address = center.Address,
            City = center.City,
            Country = center.Country,
            Latitude = center.Latitude,
            Longitude = center.Longitude,
            Description = center.Description,
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
                .OrderBy(majlis => majlis.Date)
                .Select(MapMajlis)
                .ToArray()
        };

        return OperationResult<CenterDetailsDto>.Success(dto);
    }

    public async Task<OperationResult<IReadOnlyCollection<CenterSummaryDto>>> GetCentersAsync(CancellationToken cancellationToken = default)
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
                Address = center.Address,
                City = center.City,
                Country = center.Country,
                Latitude = center.Latitude,
                Longitude = center.Longitude
            })
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<CenterSummaryDto>>.Success(centers);
    }

    public async Task<OperationResult<IReadOnlyCollection<CenterSummaryDto>>> GetNearestCentersAsync(decimal latitude, decimal longitude, CancellationToken cancellationToken = default)
    {
        var centers = await dbContext.Centers
            .AsNoTracking()
            .Select(center => new CenterSummaryDto
            {
                Id = center.Id,
                Name = center.Name,
                Address = center.Address,
                City = center.City,
                Country = center.Country,
                Latitude = center.Latitude,
                Longitude = center.Longitude
            })
            .ToArrayAsync(cancellationToken);

        var sorted = centers
            .Select(center =>
            {
                center.DistanceKm = CalculateDistanceKm((double)latitude, (double)longitude, (double)center.Latitude, (double)center.Longitude);
                return center;
            })
            .OrderBy(center => center.DistanceKm)
            .ToArray();

        return OperationResult<IReadOnlyCollection<CenterSummaryDto>>.Success(sorted);
    }

    private static MajlisDto MapMajlis(Domain.Entities.Majlis majlis)
    {
        return new MajlisDto
        {
            Id = majlis.Id,
            Title = majlis.Title,
            Description = majlis.Description,
            Date = majlis.Date,
            Time = majlis.Time,
            CenterId = majlis.CenterId,
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

    private static double CalculateDistanceKm(double lat1, double lng1, double lat2, double lng2)
    {
        const double earthRadiusKm = 6371d;
        var dLat = DegreesToRadians(lat2 - lat1);
        var dLng = DegreesToRadians(lng2 - lng1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(DegreesToRadians(lat1)) * Math.Cos(DegreesToRadians(lat2)) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return earthRadiusKm * c;
    }

    private static double DegreesToRadians(double degrees) => degrees * (Math.PI / 180d);
}
