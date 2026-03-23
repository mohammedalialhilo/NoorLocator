using System.ComponentModel.DataAnnotations;

namespace NoorLocator.Application.Management.Dtos;

public class ApproveManagerRequestDto
{
    [Range(1, int.MaxValue)]
    public int ManagerRequestId { get; set; }
}
