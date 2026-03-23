using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Suggestions.Dtos;

namespace NoorLocator.Application.Suggestions.Interfaces;

public interface ISuggestionService
{
    Task<OperationResult> CreateAsync(CreateSuggestionDto request, CancellationToken cancellationToken = default);

    Task<OperationResult<IReadOnlyCollection<SuggestionDto>>> GetAllAsync(CancellationToken cancellationToken = default);
}
