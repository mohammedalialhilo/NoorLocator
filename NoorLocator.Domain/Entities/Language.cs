using NoorLocator.Domain.Common;

namespace NoorLocator.Domain.Entities;

public class Language : Entity
{
    public string Name { get; set; } = string.Empty;

    public string Code { get; set; } = string.Empty;

    public ICollection<MajlisLanguage> MajlisLanguages { get; set; } = new List<MajlisLanguage>();

    public ICollection<CenterLanguage> CenterLanguages { get; set; } = new List<CenterLanguage>();

    public ICollection<CenterLanguageSuggestion> CenterLanguageSuggestions { get; set; } = new List<CenterLanguageSuggestion>();
}
