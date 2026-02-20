namespace OjisanBackend.Application.Common.Models;

/// <summary>
/// Configuration settings for OTO shipping API integration.
/// </summary>
public class OtoSettings
{
    /// <summary>
    /// OTO retailer ID.
    /// </summary>
    public string RetailerId { get; set; } = string.Empty;

    /// <summary>
    /// OTO API token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// OTO API base URL (e.g., "https://api.oto.sa").
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;
}
