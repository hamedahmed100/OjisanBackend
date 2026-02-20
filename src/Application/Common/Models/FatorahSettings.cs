namespace OjisanBackend.Application.Common.Models;

/// <summary>
/// Configuration settings for Fatorah payment gateway integration.
/// </summary>
public class FatorahSettings
{
    /// <summary>
    /// Fatorah API key for authentication.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Fatorah API base URL (e.g., "https://api.fatorah.com").
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Webhook secret for validating webhook signatures.
    /// </summary>
    public string WebhookSecret { get; set; } = string.Empty;
}
