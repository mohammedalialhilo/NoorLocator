namespace NoorLocator.Application.CenterImages.Dtos;

public class CenterImageDto
{
    public int Id { get; set; }

    public int CenterId { get; set; }

    public string ImageUrl { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; }

    public bool IsPrimary { get; set; }
}
