namespace NoorLocator.Application.Validation;

public sealed class ValidationResult
{
    private ValidationResult(bool isValid, IReadOnlyCollection<string> errors)
    {
        IsValid = isValid;
        Errors = errors;
    }

    public bool IsValid { get; }

    public IReadOnlyCollection<string> Errors { get; }

    public static ValidationResult Success() => new(true, Array.Empty<string>());

    public static ValidationResult Failure(params string[] errors) => new(false, errors);
}
