namespace OjisanBackend.Domain.Exceptions;

public class GroupNotAcceptingMembersException : Exception
{
    public GroupNotAcceptingMembersException(int groupId)
        : base($"Group with ID {groupId} is not currently accepting new members.")
    {
        GroupId = groupId;
    }

    public int GroupId { get; }
}
