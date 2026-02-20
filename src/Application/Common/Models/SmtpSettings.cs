namespace OjisanBackend.Application.Common.Models;

/// <summary>
/// Configuration settings for SMTP email service.
/// </summary>
public class SmtpSettings
{
    /// <summary>
    /// SMTP server hostname.
    /// </summary>
    public string Host { get; set; } = string.Empty;

    /// <summary>
    /// SMTP server port (typically 587 for TLS, 465 for SSL).
    /// </summary>
    public int Port { get; set; } = 587;

    /// <summary>
    /// SMTP username for authentication.
    /// </summary>
    public string User { get; set; } = string.Empty;

    /// <summary>
    /// SMTP password for authentication.
    /// </summary>
    public string Pass { get; set; } = string.Empty;

    /// <summary>
    /// Display name for the sender.
    /// </summary>
    public string FromName { get; set; } = "Ojisan Store";

    /// <summary>
    /// Email address for the sender.
    /// </summary>
    public string FromEmail { get; set; } = string.Empty;

    /// <summary>
    /// Whether to use SSL/TLS encryption.
    /// </summary>
    public bool EnableSsl { get; set; } = true;
}
