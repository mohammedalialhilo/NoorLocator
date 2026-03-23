using System.ComponentModel.DataAnnotations;

namespace NoorLocator.Application.Management.Dtos;

public class ManagerRequestDto
{
    [Range(1, int.MaxValue)]
    public int CenterId { get; set; }
}
