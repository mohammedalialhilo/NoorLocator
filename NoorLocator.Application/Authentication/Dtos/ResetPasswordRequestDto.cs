using System.ComponentModel.DataAnnotations;

namespace NoorLocator.Application.Authentication.Dtos;

public class ResetPasswordRequestDto
{
    [Required]
    [StringLength(512)]
    public string Token { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [StringLength(128)]
    public string Password { get; set; } = string.Empty;

    [Required]
    [MinLength(8)]
    [StringLength(128)]
    public string ConfirmPassword { get; set; } = string.Empty;
}
