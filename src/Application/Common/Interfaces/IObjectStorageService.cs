namespace OjisanBackend.Application.Common.Interfaces;

/// <summary>
/// Service for interacting with Contabo Object Storage (S3-compatible API).
/// All uploads go to your Contabo bucket; no files are stored on local disk.
/// </summary>
public interface IObjectStorageService
{
    /// <summary>
    /// Uploads a file to object storage and returns the public URL.
    /// </summary>
    /// <param name="content">File content stream.</param>
    /// <param name="fileName">Original file name (used for extension).</param>
    /// <param name="contentType">MIME type.</param>
    /// <param name="folderPrefix">Folder prefix in bucket (e.g. uploads/badges, uploads/users).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Public URL of the uploaded file.</returns>
    Task<string> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        string folderPrefix,
        CancellationToken cancellationToken);

    /// <summary>
    /// Deletes an object from storage by its key (path within bucket).
    /// </summary>
    /// <param name="objectKey">Object key (e.g. uploads/libraries/guid.jpg).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    Task DeleteAsync(string objectKey, CancellationToken cancellationToken);
}
