using OjisanBackend.Domain.Common;

namespace OjisanBackend.Domain.Events;

public class GroupAcceptedEvent : BaseEvent
{
    public GroupAcceptedEvent(int groupId)
    {
        GroupId = groupId;
        AcceptedAt = DateTime.UtcNow;
    }

    public int GroupId { get; }
    public DateTime AcceptedAt { get; }
}
