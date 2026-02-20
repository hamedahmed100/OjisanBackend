using OjisanBackend.Domain.Common;

namespace OjisanBackend.Domain.Events;

public class SecondPaymentRequestedEvent : BaseEvent
{
    public SecondPaymentRequestedEvent(int groupId, Guid paymentPublicId, string checkoutUrl)
    {
        GroupId = groupId;
        PaymentPublicId = paymentPublicId;
        CheckoutUrl = checkoutUrl;
        RequestedAt = DateTime.UtcNow;
    }

    public int GroupId { get; }
    public Guid PaymentPublicId { get; }
    public string CheckoutUrl { get; }
    public DateTime RequestedAt { get; }
}

