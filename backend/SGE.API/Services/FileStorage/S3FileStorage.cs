using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;

namespace SGE.API.Services.FileStorage;

public sealed class S3FileStorage : IFileStorage, IDisposable
{
    private readonly IAmazonS3 _client;
    private readonly string _bucket;

    public S3FileStorage(IConfiguration configuration, IHostEnvironment environment)
    {
        var endpoint = configuration["Storage:Endpoint"];
        _bucket = configuration["Storage:Bucket"] ?? string.Empty;
        var accessKey = configuration["Storage:AccessKey"] ?? string.Empty;
        var secretKey = configuration["Storage:SecretKey"] ?? string.Empty;
        var region = configuration["Storage:Region"] ?? "auto";

        if (!Uri.TryCreate(endpoint, UriKind.Absolute, out var serviceUri) ||
            (environment.IsProduction() && serviceUri.Scheme != Uri.UriSchemeHttps))
            throw new InvalidOperationException("Storage:Endpoint precisa ser uma URL HTTPS válida em produção.");
        if (string.IsNullOrWhiteSpace(_bucket) || string.IsNullOrWhiteSpace(accessKey) || string.IsNullOrWhiteSpace(secretKey))
            throw new InvalidOperationException("Storage:Bucket, Storage:AccessKey e Storage:SecretKey são obrigatórios para S3.");
        if (string.IsNullOrWhiteSpace(region))
            throw new InvalidOperationException("Storage:Region é obrigatório para S3.");

        var clientConfig = new AmazonS3Config
        {
            ServiceURL = serviceUri.ToString().TrimEnd('/'),
            ForcePathStyle = configuration.GetValue("Storage:UsePathStyle", true),
            AuthenticationRegion = region
        };
        _client = new AmazonS3Client(new BasicAWSCredentials(accessKey, secretKey), clientConfig);
    }

    public async Task<string> UploadAsync(
        Stream content,
        string keyPrefix,
        string fileExtension,
        string contentType,
        CancellationToken cancellationToken = default)
    {
        var key = StorageKey.Create(keyPrefix, fileExtension);
        await _client.PutObjectAsync(new PutObjectRequest
        {
            BucketName = _bucket,
            Key = key,
            InputStream = content,
            ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType
        }, cancellationToken);
        return key;
    }

    public async Task<Stream?> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        var safeKey = StorageKey.Normalize(key);
        try
        {
            using var response = await _client.GetObjectAsync(_bucket, safeKey, cancellationToken);
            var content = new MemoryStream();
            await response.ResponseStream.CopyToAsync(content, cancellationToken);
            content.Position = 0;
            return content;
        }
        catch (AmazonS3Exception exception) when (IsNotFound(exception))
        {
            return null;
        }
    }

    public async Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            await _client.GetObjectMetadataAsync(_bucket, StorageKey.Normalize(key), cancellationToken);
            return true;
        }
        catch (AmazonS3Exception exception) when (IsNotFound(exception))
        {
            return false;
        }
    }

    public async Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        await _client.DeleteObjectAsync(_bucket, StorageKey.Normalize(key), cancellationToken);
    }

    public void Dispose() => _client.Dispose();

    private static bool IsNotFound(AmazonS3Exception exception) =>
        exception.StatusCode == System.Net.HttpStatusCode.NotFound ||
        exception.ErrorCode is "NoSuchKey" or "NotFound";
}
