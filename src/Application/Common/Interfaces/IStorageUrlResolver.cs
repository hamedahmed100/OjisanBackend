namespace OjisanBackend.Application.Common.Interfaces;

/// <summary>
/// Resolves stored file paths or URLs to the correct public URL format.
/// For Contabo this means including the access key in the path: {endpoint}/{AccessKey}:{Bucket}/{key}.
/// </summary>
public interface IStorageUrlResolver
{
    /// <summary>
    /// Returns the public URL for the given stored value (full URL or object key).
    /// If the stored value is already in correct format, returns it unchanged.
    /// If it is in old format (e.g. without access key), rewrites to Contabo format.
    /// </summary>
    string ToPublicUrl(string storedUrlOrKey);
}
