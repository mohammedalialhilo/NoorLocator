using Microsoft.AspNetCore.Http;
using NoorLocator.Application.Common.Models;

namespace NoorLocator.Api.Extensions;

public static class FormFileExtensions
{
    public static async Task<UploadFilePayload?> ToUploadPayloadAsync(this IFormFile? file, CancellationToken cancellationToken = default)
    {
        if (file is null || file.Length == 0)
        {
            return null;
        }

        await using var stream = file.OpenReadStream();
        using var memoryStream = new MemoryStream();
        await stream.CopyToAsync(memoryStream, cancellationToken);

        return new UploadFilePayload
        {
            FileName = file.FileName,
            ContentType = file.ContentType ?? string.Empty,
            Content = memoryStream.ToArray()
        };
    }
}
