using System.ComponentModel.DataAnnotations;

namespace NoorLocator.Application.Profile.Dtos;

public class UpdateProfileDto
{
    [Required(ErrorMessage = "Display name is required.")]
    [StringLength(150, ErrorMessage = "Display name must be 150 characters or fewer.")]
    public string Name { get; set; } = string.Empty;

    [Required(ErrorMessage = "Email is required.")]
    [StringLength(256, ErrorMessage = "Email must be 256 characters or fewer.")]
    public string Email { get; set; } = string.Empty;
}
