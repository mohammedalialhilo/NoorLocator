using NoorLocator.Domain.Enums;

namespace NoorLocator.Application.Admin.Dtos;

public class UpdateAdminUserDto
{
    public string Name { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public UserRole Role { get; set; } = UserRole.User;

    public string PreferredLanguageCode { get; set; } = "en";
}
