using NoorLocator.Domain.Enums;

namespace NoorLocator.Application.Admin.Dtos;

public class AdminUserDto
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; set; }

    public int AssignedCenterCount { get; set; }

    public DateTime CreatedAt { get; set; }
}
