namespace NoorLocator.Application.Common.Configuration;

public class AzureBlobStorageSettings
{
    public const string SectionName = "AzureBlobStorage";

    public string ConnectionString { get; set; } = string.Empty;

    public string ServiceUri { get; set; } = string.Empty;

    public string AccountName { get; set; } = string.Empty;

    public string ContainerName { get; set; } = "uploads";

    public string PublicBaseUrl { get; set; } = string.Empty;

    public bool CreateContainerIfMissing { get; set; }

    public bool UseBlobPublicAccess { get; set; } = true;

    public string ManagedIdentityClientId { get; set; } = string.Empty;
}
