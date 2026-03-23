using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Languages.Dtos;

namespace NoorLocator.Application.Languages.Interfaces;

public interface ILanguageService
{
    Task<OperationResult<IReadOnlyCollection<LanguageDto>>> GetAllAsync(CancellationToken cancellationToken = default);
}
