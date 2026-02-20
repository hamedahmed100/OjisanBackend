namespace OjisanBackend.Application.Common.Interfaces;

/// <summary>
/// Service for uploading images to cloud storage.
/// </summary>
public interface IImageUploadService
{
    /// <summary>
    /// Uploads an image stream to cloud storage and returns the public URL.
    /// </summary>
    /// <param name="imageStream">The image stream to upload.</param>
    /// <param name="fileName">The original file name (used to extract extension).</param>
    /// <param name="contentType">The MIME type of the image (e.g., "image/png", "image/jpeg").</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The public URL of the uploaded image.</returns>
    Task<string> UploadImageAsync(Stream imageStream, string fileName, string contentType, CancellationToken cancellationToken);
}
