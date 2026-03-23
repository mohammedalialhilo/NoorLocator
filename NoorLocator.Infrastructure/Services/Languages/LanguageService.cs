using Microsoft.EntityFrameworkCore;
using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Languages.Dtos;
using NoorLocator.Application.Languages.Interfaces;
using NoorLocator.Infrastructure.Persistence;

namespace NoorLocator.Infrastructure.Services.Languages;

public class LanguageService(NoorLocatorDbContext dbContext) : ILanguageService
{
    public async Task<OperationResult<IReadOnlyCollection<LanguageDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var languages = await dbContext.Languages
            .AsNoTracking()
            .OrderBy(language => language.Name)
            .Select(language => new LanguageDto
            {
                Id = language.Id,
                Name = language.Name,
                Code = language.Code
            })
            .ToArrayAsync(cancellationToken);

        return OperationResult<IReadOnlyCollection<LanguageDto>>.Success(languages);
    }
}
