using System.ComponentModel.DataAnnotations;

namespace NoorLocator.Application.CenterImages.Dtos;

public class UploadCenterImageDto
{
    [Range(1, int.MaxValue)]
    public int CenterId { get; set; }

    public bool IsPrimary { get; set; }
}
