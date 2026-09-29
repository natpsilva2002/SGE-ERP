using System.Text.RegularExpressions;

namespace SGE.API.Services.FileStorage;

internal static partial class StorageKey
{
    private const int MaximumKeyLength = 500;

    public static string Create(string keyPrefix, string extension)
    {
        var prefixSegments = (keyPrefix ?? string.Empty).Replace('\\', '/').Split('/');
        if (prefixSegments.Length == 0 || prefixSegments.Any(segment => !IsSafeSegment(segment)))
            throw new ArgumentException("Prefixo de armazenamento inválido.", nameof(keyPrefix));

        var safeExtension = extension ?? string.Empty;
        if (!SafeExtensionRegex().IsMatch(safeExtension))
            throw new ArgumentException("Extensão de arquivo inválida.", nameof(extension));

        var key = $"{string.Join('/', prefixSegments)}/{Guid.NewGuid():N}{safeExtension.ToLowerInvariant()}";
        if (key.Length > MaximumKeyLength)
            throw new ArgumentException("Chave de armazenamento muito longa.", nameof(keyPrefix));

        return key;
    }

    public static string Normalize(string key)
    {
        if (string.IsNullOrWhiteSpace(key) || key.Length > MaximumKeyLength || Path.IsPathRooted(key))
            throw new InvalidOperationException("Chave de anexo inválida.");

        var normalized = key.Replace('\\', '/');
        var segments = normalized.Split('/');

        // Older database rows use uploads/<category>/<random-name>.
        if (segments.Length >= 3 && segments[0].Equals("uploads", StringComparison.OrdinalIgnoreCase))
            segments = segments[1..];

        if (segments.Length < 2 || segments.Any(segment => !IsSafeSegment(segment)))
            throw new InvalidOperationException("Chave de anexo inválida.");

        return string.Join('/', segments);
    }

    private static bool IsSafeSegment(string segment) =>
        segment is not ("." or "..") && SafeSegmentRegex().IsMatch(segment);

    [GeneratedRegex("^[A-Za-z0-9._-]+$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeSegmentRegex();

    [GeneratedRegex("^\\.[A-Za-z0-9]{1,10}$", RegexOptions.CultureInvariant)]
    private static partial Regex SafeExtensionRegex();
}
