using System.ComponentModel.DataAnnotations;

namespace NoorLocator.Application.Authentication.Dtos;

public class ResendVerificationEmailRequestDto
{
    [EmailAddress]
    [StringLength(256)]
    public string Email { get; set; } = string.Empty;
}
