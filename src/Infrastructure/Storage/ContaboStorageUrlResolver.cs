using Microsoft.Extensions.Options;
using OjisanBackend.Application.Common.Interfaces;

namespace OjisanBackend.Infrastructure.Storage;

/// <summary>
/// Resolves stored URLs to Contabo public format: {Endpoint}/{AccessKey}:{Bucket}/{key}.
/// Rewrites old-format URLs (without access key in path) so they work when returned from the API.
/// </summary>
public class ContaboStorageUrlResolver : IStorageUrlResolver
{
    private readonly S3StorageOptions _options;

    public ContaboStorageUrlResolver(IOptions<S3StorageOptions> options)
    {
        _options = options.Value;
    }

    public string ToPublicUrl(string storedUrlOrKey)
    {
        if (string.IsNullOrWhiteSpace(storedUrlOrKey))
            return storedUrlOrKey;

        if (string.IsNullOrWhiteSpace(_options.AccessKey))
            return storedUrlOrKey;

        var endpoint = _options.Endpoint.TrimEnd('/');
        var bucket = _options.Bucket;

        // Already in Contabo format: contains ":{Bucket}/"
        if (storedUrlOrKey.Contains($":{bucket}/", StringComparison.OrdinalIgnoreCase))
            return storedUrlOrKey;

        // Full URL in old format: https://eu2.contabostorage.com/photos/uploads/...
        if (storedUrlOrKey.StartsWith(endpoint, StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var path = new Uri(storedUrlOrKey).AbsolutePath.TrimStart('/');
                var bucketPrefix = $"{bucket}/";
                if (path.StartsWith(bucketPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var key = path[bucketPrefix.Length..];
                    return $"{endpoint}/{_options.AccessKey}:{bucket}/{key}";
                }
            }
            catch
            {
                return storedUrlOrKey;
            }
        }

        // Relative path or key only (e.g. uploads/libraries/guid.png)
        if (!storedUrlOrKey.StartsWith("http", StringComparison.OrdinalIgnoreCase))
        {
            var key = storedUrlOrKey.TrimStart('/');
            return $"{endpoint}/{_options.AccessKey}:{bucket}/{key}";
        }

        return storedUrlOrKey;
    }
}
