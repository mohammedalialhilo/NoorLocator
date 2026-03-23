namespace NoorLocator.Application.Authentication.Dtos;

public class CurrentUserDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Role { get; set; } = string.Empty;

    public IReadOnlyCollection<int> AssignedCenterIds { get; set; } = Array.Empty<int>();
}
