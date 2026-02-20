namespace OjisanBackend.Application.Common.Interfaces;

/// <summary>
/// Service for sending WhatsApp notifications.
/// </summary>
public interface IWhatsAppService
{
    /// <summary>
    /// Sends a WhatsApp message using a template.
    /// </summary>
    /// <param name="toPhoneNumber">Recipient phone number (should include country code, e.g., +966501234567).</param>
    /// <param name="templateName">Name of the WhatsApp template to use.</param>
    /// <param name="parameters">Template parameters to fill in the template placeholders.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Task representing the async operation.</returns>
    Task SendMessageAsync(string toPhoneNumber, string templateName, Dictionary<string, string> parameters, CancellationToken cancellationToken);
}
