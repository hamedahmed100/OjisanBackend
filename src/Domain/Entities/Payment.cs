using OjisanBackend.Domain.Common;
using OjisanBackend.Domain.Enums;
using OjisanBackend.Domain.Events;

namespace OjisanBackend.Domain.Entities;

public class Payment : BaseAuditableEntity
{
    /// <summary>
    /// Public identifier for the payment. Use this value in APIs.
    /// </summary>
    public Guid PublicId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the Group entity.
    /// </summary>
    public int GroupId { get; set; }

    /// <summary>
    /// Transaction ID from Fatorah payment gateway.
    /// </summary>
    public string TransactionId { get; set; } = string.Empty;

    /// <summary>
    /// Payment amount.
    /// </summary>
    public decimal Amount { get; set; }

    /// <summary>
    /// Whether this is a partial payment (50% split) or full payment.
    /// </summary>
    public bool IsPartial { get; set; }

    /// <summary>
    /// Indicates which phase this payment belongs to (Upfront or Final).
    /// </summary>
    public PaymentPhase Phase { get; set; } = PaymentPhase.Upfront;

    /// <summary>
    /// Current status of the payment.
    /// </summary>
    public PaymentStatus Status { get; set; } = PaymentStatus.Pending;

    public void MarkFinalPaymentRequested(string checkoutUrl)
    {
        if (Phase != PaymentPhase.Final)
        {
            return;
        }

        AddDomainEvent(new SecondPaymentRequestedEvent(GroupId, PublicId, checkoutUrl));
    }
}
