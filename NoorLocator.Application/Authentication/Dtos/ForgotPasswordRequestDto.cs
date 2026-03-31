using System.ComponentModel.DataAnnotations;

namespace NoorLocator.Application.Authentication.Dtos;

public class ForgotPasswordRequestDto
{
    [Required]
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
}
