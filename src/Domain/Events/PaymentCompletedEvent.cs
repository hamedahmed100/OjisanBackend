using OjisanBackend.Domain.Common;

namespace OjisanBackend.Domain.Events;

public class PaymentCompletedEvent : BaseEvent
{
    public PaymentCompletedEvent(int groupId, Guid paymentPublicId)
    {
        GroupId = groupId;
        PaymentPublicId = paymentPublicId;
        CompletedAt = DateTime.UtcNow;
    }

    public int GroupId { get; }
    public Guid PaymentPublicId { get; }
    public DateTime CompletedAt { get; }
}
