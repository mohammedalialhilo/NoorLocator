using Azure.Identity;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Options;
using NoorLocator.Application.Common.Configuration;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Infrastructure.Services.Media;

public class AzureBlobStorageService(
    IOptions<MediaStorageSettings> mediaStorageOptions,
    IOptions<AzureBlobStorageSettings> azureBlobStorageOptions) : IMediaStorageService
{
    private readonly MediaStorageSettings mediaStorageSettings = mediaStorageOptions.Value;
    private readonly AzureBlobStorageSettings azureBlobStorageSettings = azureBlobStorageOptions.Value;

    public async Task<OperationResult<StoredMediaFile>> SaveImageAsync(
        UploadFilePayload file,
        string category,
        CancellationToken cancellationToken = default)
    {
        var validation = MediaImageValidation.Validate(file, mediaStorageSettings.MaxImageSizeBytes);
        if (!validation.Succeeded)
        {
            return OperationResult<StoredMediaFile>.Failure(validation.Message, validation.StatusCode);
        }

        var normalizedCategory = MediaStorageFileNameGenerator.NormalizeCategory(category);
        var safeFileName = MediaStorageFileNameGenerator.GenerateSafeImageFileName(file.FileName);
        var blobName = $"{normalizedCategory}/{safeFileName}";
        var extension = Path.GetExtension(safeFileName)?.Trim().ToLowerInvariant() ?? string.Empty;

        var containerClient = await GetContainerClientAsync(cancellationToken);
        var blobClient = containerClient.GetBlobClient(blobName);
        await blobClient.UploadAsync(
            BinaryData.FromBytes(file.Content),
            new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders
                {
                    ContentType = MediaImageValidation.GetContentType(extension)
                }
            },
            cancellationToken);

        return OperationResult<StoredMediaFile>.Success(
            new StoredMediaFile
            {
                PublicUrl = BuildPublicUrl(blobClient.Uri, blobName),
                ContentType = MediaImageValidation.GetContentType(extension),
                SizeBytes = file.Content.LongLength
            },
            "Image stored successfully.",
            201);
    }

    public async Task DeleteFileAsync(string? publicUrl, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(publicUrl))
        {
            return;
        }

        var containerClient = GetContainerClient();
        var blobName = TryResolveBlobName(publicUrl, containerClient);
        if (string.IsNullOrWhiteSpace(blobName))
        {
            return;
        }

        await containerClient.DeleteBlobIfExistsAsync(blobName, cancellationToken: cancellationToken);
    }

    private async Task<BlobContainerClient> GetContainerClientAsync(CancellationToken cancellationToken)
    {
        var containerClient = GetContainerClient();
        if (azureBlobStorageSettings.CreateContainerIfMissing)
        {
            await containerClient.CreateIfNotExistsAsync(
                publicAccessType: azureBlobStorageSettings.UseBlobPublicAccess ? PublicAccessType.Blob : PublicAccessType.None,
                cancellationToken: cancellationToken);
        }

        return containerClient;
    }

    private BlobContainerClient GetContainerClient()
    {
        if (!string.IsNullOrWhiteSpace(azureBlobStorageSettings.ConnectionString))
        {
            return new BlobContainerClient(
                azureBlobStorageSettings.ConnectionString,
                azureBlobStorageSettings.ContainerName);
        }

        var serviceUri = ResolveServiceUri();
        var credentialOptions = new DefaultAzureCredentialOptions();
        if (!string.IsNullOrWhiteSpace(azureBlobStorageSettings.ManagedIdentityClientId))
        {
            credentialOptions.ManagedIdentityClientId = azureBlobStorageSettings.ManagedIdentityClientId;
        }

        var serviceClient = new BlobServiceClient(serviceUri, new DefaultAzureCredential(credentialOptions));
        return serviceClient.GetBlobContainerClient(azureBlobStorageSettings.ContainerName);
    }

    private Uri ResolveServiceUri()
    {
        if (Uri.TryCreate(azureBlobStorageSettings.ServiceUri, UriKind.Absolute, out var serviceUri))
        {
            return serviceUri;
        }

        if (!string.IsNullOrWhiteSpace(azureBlobStorageSettings.AccountName))
        {
            return new Uri($"https://{azureBlobStorageSettings.AccountName}.blob.core.windows.net");
        }

        throw new InvalidOperationException("Azure blob storage requires either AzureBlobStorage:ConnectionString, AzureBlobStorage:ServiceUri, or AzureBlobStorage:AccountName.");
    }

    private string BuildPublicUrl(Uri blobUri, string blobName)
    {
        var configuredBaseUrl = azureBlobStorageSettings.PublicBaseUrl?.Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl))
        {
            return $"{configuredBaseUrl}/{blobName}";
        }

        return blobUri.AbsoluteUri;
    }

    private string? TryResolveBlobName(string publicUrl, BlobContainerClient containerClient)
    {
        var configuredBaseUrl = azureBlobStorageSettings.PublicBaseUrl?.Trim().TrimEnd('/');
        if (!string.IsNullOrWhiteSpace(configuredBaseUrl) &&
            publicUrl.StartsWith($"{configuredBaseUrl}/", StringComparison.OrdinalIgnoreCase))
        {
            return publicUrl[(configuredBaseUrl.Length + 1)..];
        }

        if (!Uri.TryCreate(publicUrl, UriKind.Absolute, out var publicUri))
        {
            return null;
        }

        var containerUri = containerClient.Uri.AbsoluteUri.TrimEnd('/');
        if (!publicUri.AbsoluteUri.StartsWith($"{containerUri}/", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return publicUri.AbsoluteUri[(containerUri.Length + 1)..];
    }
}
