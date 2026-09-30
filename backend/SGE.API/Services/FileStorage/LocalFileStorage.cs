namespace SGE.API.Services.FileStorage;

public sealed class LocalFileStorage : IFileStorage
{
    private readonly string _root;

    public LocalFileStorage(IWebHostEnvironment environment, IConfiguration configuration)
    {
        var configuredPath = configuration["UPLOADS_PATH"];
        _root = Path.GetFullPath(string.IsNullOrWhiteSpace(configuredPath)
            ? Path.Combine(environment.ContentRootPath, "uploads")
            : configuredPath);
    }

    public async Task<string> UploadAsync(
        Stream content,
        string keyPrefix,
        string fileExtension,
        string contentType,
        CancellationToken cancellationToken = default,
        long? contentLength = null)
    {
        var key = StorageKey.Create(keyPrefix, fileExtension);
        var path = Resolve(key);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);

        try
        {
            await using var target = new FileStream(path, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true);
            await content.CopyToAsync(target, cancellationToken);
            return key;
        }
        catch
        {
            if (File.Exists(path)) File.Delete(path);
            throw;
        }
    }

    public Task<Stream?> DownloadAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Resolve(key);
        Stream? result = File.Exists(path)
            ? new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true)
            : null;
        return Task.FromResult(result);
    }

    public Task<bool> ExistsAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(File.Exists(Resolve(key)));
    }

    public Task DeleteAsync(string key, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = Resolve(key);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string Resolve(string key)
    {
        var normalized = StorageKey.Normalize(key);
        var path = Path.GetFullPath(Path.Combine(_root, normalized.Replace('/', Path.DirectorySeparatorChar)));
        var relative = Path.GetRelativePath(_root, path);
        if (relative == ".." || relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            throw new InvalidOperationException("Chave de anexo inválida.");
        return path;
    }
}
