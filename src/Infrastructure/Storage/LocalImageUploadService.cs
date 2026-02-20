using Microsoft.Extensions.Configuration;
using OjisanBackend.Application.Common.Interfaces;

namespace OjisanBackend.Infrastructure.Storage;

/// <summary>
/// Implementation of IImageUploadService using local file system storage.
/// Files are stored on disk and served via Nginx.
/// </summary>
public class LocalImageUploadService : IImageUploadService
{
    private readonly IConfiguration _configuration;
    private readonly string _storagePath;
    private readonly string _baseUrl;

    public LocalImageUploadService(IConfiguration configuration)
    {
        _configuration = configuration;
        _storagePath = _configuration["ImageUpload:StoragePath"]
            ?? throw new InvalidOperationException("ImageUpload:StoragePath is not configured in appsettings.json.");
        _baseUrl = _configuration["ImageUpload:BaseUrl"]
            ?? throw new InvalidOperationException("ImageUpload:BaseUrl is not configured in appsettings.json.");
    }

    public async Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType, CancellationToken cancellationToken)
    {
        // Extract and sanitize extension to prevent path traversal attacks
        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrWhiteSpace(extension))
        {
            // Default to .png if no extension found
            extension = contentType == "image/jpeg" ? ".jpg" : ".png";
        }

        // Sanitize extension - only allow image extensions
        extension = Path.GetExtension(extension).ToLowerInvariant();
        if (extension != ".png" && extension != ".jpg" && extension != ".jpeg")
        {
            throw new ArgumentException("Invalid file extension. Only PNG and JPEG are allowed.", nameof(fileName));
        }

        // Generate unique filename using GUID to prevent overwrites and path traversal attacks
        var uniqueFileName = $"{Guid.NewGuid()}{extension}";

        // Ensure the storage directory exists
        if (!Directory.Exists(_storagePath))
        {
            Directory.CreateDirectory(_storagePath);
        }

        // Combine the storage path with the unique filename
        var fullPath = Path.Combine(_storagePath, uniqueFileName);

        // Ensure the full path is within the storage directory (additional security check)
        var normalizedStoragePath = Path.GetFullPath(_storagePath);
        var normalizedFullPath = Path.GetFullPath(fullPath);
        
        if (!normalizedFullPath.StartsWith(normalizedStoragePath, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Invalid file path detected. Path traversal attack prevented.");
        }

        // Copy the stream to the file
        await using var fileStream = new FileStream(fullPath, FileMode.Create, FileAccess.Write, FileShare.None, bufferSize: 4096, useAsync: true);
        await imageStream.CopyToAsync(fileStream, cancellationToken);
        await fileStream.FlushAsync(cancellationToken);

        // Return the URL path (relative or absolute depending on configuration)
        var urlPath = _baseUrl.TrimEnd('/') + "/" + uniqueFileName;
        return urlPath;
    }
}
