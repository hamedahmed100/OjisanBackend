using OjisanBackend.Domain.Common;

namespace OjisanBackend.Domain.Events;

public class PaymentCompletedEvent : BaseEvent
{
    public PaymentCompletedEvent(int? groupId, int? orderSubmissionId, Guid paymentPublicId)
    {
        GroupId = groupId;
        OrderSubmissionId = orderSubmissionId;
        PaymentPublicId = paymentPublicId;
        CompletedAt = DateTime.UtcNow;
    }

    public int? GroupId { get; }
    public int? OrderSubmissionId { get; }
    public Guid PaymentPublicId { get; }
    public DateTime CompletedAt { get; }
}
