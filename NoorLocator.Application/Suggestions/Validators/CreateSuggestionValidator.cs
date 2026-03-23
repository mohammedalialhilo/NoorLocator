using NoorLocator.Application.Suggestions.Dtos;
using NoorLocator.Application.Validation;

namespace NoorLocator.Application.Suggestions.Validators;

public class CreateSuggestionValidator : IValidator<CreateSuggestionDto>
{
    public ValidationResult Validate(CreateSuggestionDto instance)
    {
        return string.IsNullOrWhiteSpace(instance.Message)
            ? ValidationResult.Failure("Suggestion message is required.")
            : ValidationResult.Success();
    }
}
