using NoorLocator.Domain.Common;

namespace NoorLocator.Domain.Entities;

public class AppContent : Entity
{
    public string Key { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;

    public string LanguageCode { get; set; } = "en";
}
