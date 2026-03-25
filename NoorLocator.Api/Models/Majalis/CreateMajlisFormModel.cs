using Microsoft.AspNetCore.Http;

namespace NoorLocator.Api.Models.Majalis;

public class CreateMajlisFormModel
{
    public string Title { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public DateTime Date { get; set; }

    public string Time { get; set; } = string.Empty;

    public int CenterId { get; set; }

    public List<int> LanguageIds { get; set; } = [];

    public IFormFile? Image { get; set; }
}
