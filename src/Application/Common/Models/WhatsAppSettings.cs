namespace OjisanBackend.Application.Common.Models;

/// <summary>
/// Configuration settings for WhatsApp messaging service.
/// </summary>
public class WhatsAppSettings
{
    /// <summary>
    /// WhatsApp API base URL (e.g., "https://api.twilio.com" or "https://api.360dialog.com").
    /// </summary>
    public string ApiUrl { get; set; } = string.Empty;

    /// <summary>
    /// API token for authentication.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Sender WhatsApp number (e.g., "+966501234567").
    /// </summary>
    public string SenderNumber { get; set; } = string.Empty;
}
