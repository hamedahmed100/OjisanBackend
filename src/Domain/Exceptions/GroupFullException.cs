namespace OjisanBackend.Domain.Exceptions;

public class GroupFullException : Exception
{
    public GroupFullException(int groupId, int currentMembers, int maxMembers)
        : base($"Group with ID {groupId} is full. Current members: {currentMembers}/{maxMembers}")
    {
        GroupId = groupId;
        CurrentMembers = currentMembers;
        MaxMembers = maxMembers;
    }

    public int GroupId { get; }
    public int CurrentMembers { get; }
    public int MaxMembers { get; }
}
