using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;

namespace NoorLocator.IntegrationTests;

internal sealed class FakeBlobStorageServer : IAsyncDisposable
{
    private const string AccountName = "devstoreaccount1";
    private const string AccountKey = "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA=";

    private readonly HttpListener listener = new();
    private readonly CancellationTokenSource shutdown = new();
    private readonly Task processingTask;
    private readonly ConcurrentDictionary<string, StoredBlob> blobs = new(StringComparer.OrdinalIgnoreCase);

    private int containerCreateRequestCount;
    private int uploadRequestCount;
    private int deleteRequestCount;

    public FakeBlobStorageServer()
    {
        Port = GetAvailablePort();
        ServiceBaseUrl = $"http://127.0.0.1:{Port}/{AccountName}";
        ConnectionString = $"DefaultEndpointsProtocol=http;AccountName={AccountName};AccountKey={AccountKey};BlobEndpoint={ServiceBaseUrl};";

        listener.Prefixes.Add($"http://127.0.0.1:{Port}/");
        listener.Start();
        processingTask = Task.Run(ProcessRequestsAsync);
    }

    public int Port { get; }

    public string ServiceBaseUrl { get; }

    public string ConnectionString { get; }

    public int ContainerCreateRequestCount => containerCreateRequestCount;

    public int UploadRequestCount => uploadRequestCount;

    public int DeleteRequestCount => deleteRequestCount;

    public bool PublicBlobAccessWasRequested { get; private set; }

    public string GetContainerBaseUrl(string containerName)
    {
        return $"{ServiceBaseUrl.TrimEnd('/')}/{containerName.Trim('/')}";
    }

    public async Task<byte[]?> GetBlobContentAsync(string blobUrl)
    {
        var key = NormalizeBlobKey(blobUrl);
        return blobs.TryGetValue(key, out var blob) ? blob.Content : null;
    }

    private async Task ProcessRequestsAsync()
    {
        while (!shutdown.IsCancellationRequested)
        {
            HttpListenerContext? context = null;

            try
            {
                context = await listener.GetContextAsync().WaitAsync(shutdown.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (HttpListenerException) when (shutdown.IsCancellationRequested)
            {
                break;
            }

            await HandleRequestAsync(context);
        }
    }

    private async Task HandleRequestAsync(HttpListenerContext context)
    {
        try
        {
            ApplyCommonHeaders(context.Response);

            var request = context.Request;
            var blobKey = NormalizeBlobKey(request.Url?.AbsoluteUri ?? string.Empty);
            var isContainerCreate = string.Equals(request.HttpMethod, "PUT", StringComparison.OrdinalIgnoreCase) &&
                                    string.Equals(request.QueryString["restype"], "container", StringComparison.OrdinalIgnoreCase);

            if (isContainerCreate)
            {
                Interlocked.Increment(ref containerCreateRequestCount);
                PublicBlobAccessWasRequested = string.Equals(request.Headers["x-ms-blob-public-access"], "blob", StringComparison.OrdinalIgnoreCase);
                context.Response.StatusCode = (int)HttpStatusCode.Created;
                return;
            }

            if (string.Equals(request.HttpMethod, "PUT", StringComparison.OrdinalIgnoreCase))
            {
                using var memoryStream = new MemoryStream();
                await request.InputStream.CopyToAsync(memoryStream);

                blobs[blobKey] = new StoredBlob(
                    memoryStream.ToArray(),
                    request.Headers["x-ms-blob-content-type"] ?? request.ContentType ?? "application/octet-stream");

                Interlocked.Increment(ref uploadRequestCount);
                context.Response.StatusCode = (int)HttpStatusCode.Created;
                context.Response.Headers["ETag"] = "\"fake-etag\"";
                return;
            }

            if (string.Equals(request.HttpMethod, "DELETE", StringComparison.OrdinalIgnoreCase))
            {
                blobs.TryRemove(blobKey, out _);
                Interlocked.Increment(ref deleteRequestCount);
                context.Response.StatusCode = (int)HttpStatusCode.Accepted;
                return;
            }

            if (string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase) ||
                string.Equals(request.HttpMethod, "HEAD", StringComparison.OrdinalIgnoreCase))
            {
                if (!blobs.TryGetValue(blobKey, out var blob))
                {
                    context.Response.StatusCode = (int)HttpStatusCode.NotFound;
                    return;
                }

                context.Response.StatusCode = (int)HttpStatusCode.OK;
                context.Response.ContentType = blob.ContentType;
                context.Response.ContentLength64 = blob.Content.LongLength;

                if (string.Equals(request.HttpMethod, "GET", StringComparison.OrdinalIgnoreCase))
                {
                    await context.Response.OutputStream.WriteAsync(blob.Content);
                }

                return;
            }

            context.Response.StatusCode = (int)HttpStatusCode.NotImplemented;
        }
        finally
        {
            context.Response.OutputStream.Close();
            context.Response.Close();
        }
    }

    private static void ApplyCommonHeaders(HttpListenerResponse response)
    {
        response.Headers["x-ms-request-id"] = Guid.NewGuid().ToString("N");
        response.Headers["x-ms-version"] = "2024-11-04";
        response.Headers["Date"] = DateTimeOffset.UtcNow.ToString("R");
        response.Headers["Last-Modified"] = DateTimeOffset.UtcNow.ToString("R");
    }

    private static int GetAvailablePort()
    {
        using var tcpListener = new TcpListener(IPAddress.Loopback, 0);
        tcpListener.Start();
        return ((IPEndPoint)tcpListener.LocalEndpoint).Port;
    }

    private static string NormalizeBlobKey(string absoluteUrl)
    {
        if (!Uri.TryCreate(absoluteUrl, UriKind.Absolute, out var uri))
        {
            return string.Empty;
        }

        var trimmedPath = uri.AbsolutePath.Trim('/');
        if (trimmedPath.StartsWith($"{AccountName}/", StringComparison.OrdinalIgnoreCase))
        {
            trimmedPath = trimmedPath[(AccountName.Length + 1)..];
        }

        return Uri.UnescapeDataString(trimmedPath);
    }

    public async ValueTask DisposeAsync()
    {
        shutdown.Cancel();

        if (listener.IsListening)
        {
            listener.Stop();
        }

        listener.Close();

        try
        {
            await processingTask;
        }
        catch (OperationCanceledException)
        {
        }
        catch (HttpListenerException)
        {
        }

        shutdown.Dispose();
    }

    private sealed record StoredBlob(byte[] Content, string ContentType);
}
