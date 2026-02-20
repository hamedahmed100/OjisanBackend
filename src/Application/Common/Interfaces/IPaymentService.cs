namespace OjisanBackend.Application.Common.Interfaces;

/// <summary>
/// Service for integrating with payment gateways (e.g., Fatorah).
/// </summary>
public interface IPaymentService
{
    /// <summary>
    /// Creates a payment session with the payment gateway and returns the checkout URL.
    /// </summary>
    /// <param name="groupId">The group ID to associate with this payment.</param>
    /// <param name="amount">The payment amount.</param>
    /// <param name="merchantResourceId">A unique identifier to track this payment (typically Group PublicId or Payment PublicId).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The payment gateway checkout URL.</returns>
    Task<string> CreatePaymentSessionAsync(Guid groupId, decimal amount, string merchantResourceId, CancellationToken cancellationToken);

    /// <summary>
    /// Validates the webhook signature to ensure the request came from the payment gateway.
    /// </summary>
    /// <param name="payload">The raw request body as a string.</param>
    /// <param name="signature">The signature from the webhook header.</param>
    /// <returns>True if the signature is valid, false otherwise.</returns>
    bool ValidateWebhookSignature(string payload, string signature);
}
