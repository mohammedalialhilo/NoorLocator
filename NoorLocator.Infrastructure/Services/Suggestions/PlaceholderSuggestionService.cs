using NoorLocator.Application.Common.Models;
using NoorLocator.Application.Suggestions.Dtos;
using NoorLocator.Application.Suggestions.Interfaces;

namespace NoorLocator.Infrastructure.Services.Suggestions;

public class PlaceholderSuggestionService : ISuggestionService
{
    public Task<OperationResult> CreateAsync(CreateSuggestionDto request, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(
            OperationResult.Accepted(
                "Feedback submission is scaffolded. Stored moderation will be implemented in the next phase."));
    }

    public Task<OperationResult<IReadOnlyCollection<SuggestionDto>>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IReadOnlyCollection<SuggestionDto> suggestions = Array.Empty<SuggestionDto>();

        return Task.FromResult(
            OperationResult<IReadOnlyCollection<SuggestionDto>>.Success(
                suggestions,
                "Admin suggestion review is scaffolded and currently returns an empty set."));
    }
}
