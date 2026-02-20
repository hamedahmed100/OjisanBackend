using OjisanBackend.Domain.Common;

namespace OjisanBackend.Domain.Events;

public class MemberJoinedGroupEvent : BaseEvent
{
    public MemberJoinedGroupEvent(int groupId, string userId)
    {
        GroupId = groupId;
        UserId = userId;
        JoinedAt = DateTime.UtcNow;
    }

    public int GroupId { get; }
    public string UserId { get; }
    public DateTime JoinedAt { get; }
}
