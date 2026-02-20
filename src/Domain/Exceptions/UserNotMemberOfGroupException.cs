namespace OjisanBackend.Domain.Exceptions;

public class UserNotMemberOfGroupException : Exception
{
    public UserNotMemberOfGroupException(int groupId, string userId)
        : base($"User {userId} is not a member of group {groupId}.")
    {
        GroupId = groupId;
        UserId = userId;
    }

    public int GroupId { get; }
    public string UserId { get; }
}
