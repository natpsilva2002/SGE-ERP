namespace SGE.API.Services.FileStorage;

public interface IFileStorage
{
    Task<string> UploadAsync(
        Stream content,
        string keyPrefix,
        string fileExtension,
        string contentType,
        CancellationToken cancellationToken = default);

    Task<Stream?> DownloadAsync(string key, CancellationToken cancellationToken = default);

    Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default);

    Task DeleteAsync(string key, CancellationToken cancellationToken = default);
}
