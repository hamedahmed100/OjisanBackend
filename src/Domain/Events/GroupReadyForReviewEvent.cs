using OjisanBackend.Domain.Common;

namespace OjisanBackend.Domain.Events;

public class GroupReadyForReviewEvent : BaseEvent
{
    public GroupReadyForReviewEvent(int groupId)
    {
        GroupId = groupId;
        ReadyAt = DateTime.UtcNow;
    }

    public int GroupId { get; }
    public DateTime ReadyAt { get; }
}
