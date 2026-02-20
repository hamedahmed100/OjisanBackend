namespace OjisanBackend.Domain.Exceptions;

public class MemberAlreadyInGroupException : Exception
{
    public MemberAlreadyInGroupException(int groupId, string userId)
        : base($"User {userId} is already a member of group {groupId}.")
    {
        GroupId = groupId;
        UserId = userId;
    }

    public int GroupId { get; }
    public string UserId { get; }
}
