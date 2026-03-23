using Microsoft.EntityFrameworkCore;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Languages.Dtos;
using NoorLocator.Application.Majalis.Dtos;
using NoorLocator.Application.Majalis.Interfaces;
using NoorLocator.Domain.Entities;
using NoorLocator.Infrastructure.Persistence;

namespace NoorLocator.Infrastructure.Services.Majalis;

public class MajlisService(NoorLocatorDbContext dbContext) : IMajlisService
{
    public async Task<OperationResult> CreateMajlisAsync(CreateMajlisDto request, int userId, bool isAdmin, CancellationToken cancellationToken = default)
    {
        var centerExists = await dbContext.Centers.AnyAsync(center => center.Id == request.CenterId, cancellationToken);
        if (!centerExists)
        {
            return OperationResult.Failure("Center not found.", 404);
        }

        var distinctLanguageIds = request.LanguageIds.Distinct().ToArray();
        var knownLanguageCount = await dbContext.Languages.CountAsync(language => distinctLanguageIds.Contains(language.Id), cancellationToken);
        if (distinctLanguageIds.Length != knownLanguageCount)
        {
            return OperationResult.Failure("Majlis languages must come from the predefined language table.", 400);
        }

        if (!isAdmin)
        {
            var hasAssignment = await dbContext.CenterManagers.AnyAsync(
                centerManager => centerManager.UserId == userId &&
                                 centerManager.CenterId == request.CenterId &&
                                 centerManager.Approved,
                cancellationToken);

            if (!hasAssignment)
            {
                return OperationResult.Failure("Managers can only publish majalis for assigned centers.", 403);
            }
        }

        var majlis = new Domain.Entities.Majlis
        {
            Title = request.Title.Trim(),
            Description = request.Description.Trim(),
            Date = request.Date,
            Time = request.Time.Trim(),
            CenterId = request.CenterId,
            CreatedByManagerId = userId,
            CreatedAt = DateTime.UtcNow
        };

        dbContext.Majalis.Add(majlis);
        await dbContext.SaveChangesAsync(cancellationToken);

        if (distinctLanguageIds.Length > 0)
        {
            dbContext.MajlisLanguages.AddRange(
                distinctLanguageIds.Select(languageId => new MajlisLanguage
                {
                    MajlisId = majlis.Id,
                    LanguageId = languageId
                }));

            await dbContext.SaveChangesAsync(cancellationToken);
        }

        return OperationResult.Success("Majlis created successfully.", 201);
    }

    public async Task<OperationResult<IReadOnlyCollection<MajlisDto>>> GetMajalisAsync(int? centerId, CancellationToken cancellationToken = default)
    {
        var query = dbContext.Majalis
            .AsNoTracking()
            .Include(majlis => majlis.MajlisLanguages)
                .ThenInclude(majlisLanguage => majlisLanguage.Language)
            .AsQueryable();

        if (centerId.HasValue)
        {
            query = query.Where(majlis => majlis.CenterId == centerId.Value);
        }

        var majalis = await query
            .OrderBy(majlis => majlis.Date)
            .Select(majlis => new MajlisDto
            {
                Id = majlis.Id,
                Title = majlis.Title,
                Description = majlis.Description,
                Date = majlis.Date,
                Time = majlis.Time,
                CenterId = majlis.CenterId,
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
}
