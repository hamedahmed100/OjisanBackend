using OjisanBackend.Domain.Common;

namespace OjisanBackend.Domain.Events;

public class GroupRejectedEvent : BaseEvent
{
    public GroupRejectedEvent(int groupId)
    {
        GroupId = groupId;
        RejectedAt = DateTime.UtcNow;
    }

    public int GroupId { get; }
    public DateTime RejectedAt { get; }
}
