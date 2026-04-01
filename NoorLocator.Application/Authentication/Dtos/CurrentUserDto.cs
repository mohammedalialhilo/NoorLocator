namespace NoorLocator.Application.Authentication.Dtos;

public class CurrentUserDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string PreferredLanguageCode { get; set; } = "en";

    public bool IsEmailVerified { get; set; }

    public string Role { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public DateTime? LastLoginAtUtc { get; set; }

    public DateTime? UpdatedAtUtc { get; set; }

    public IReadOnlyCollection<int> AssignedCenterIds { get; set; } = Array.Empty<int>();
}
