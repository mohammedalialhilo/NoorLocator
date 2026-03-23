namespace NoorLocator.Infrastructure.Services.Media;

public class StoredMediaFile
{
    public string PublicUrl { get; set; } = string.Empty;

    public string ContentType { get; set; } = string.Empty;

    public long SizeBytes { get; set; }
}
