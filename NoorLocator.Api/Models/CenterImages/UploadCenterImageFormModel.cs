using Microsoft.AspNetCore.Http;

namespace NoorLocator.Api.Models.CenterImages;

public class UploadCenterImageFormModel
{
    public int CenterId { get; set; }

    public bool IsPrimary { get; set; }

    public IFormFile? Image { get; set; }
}
