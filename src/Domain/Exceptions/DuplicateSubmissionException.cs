namespace OjisanBackend.Domain.Exceptions;

public class DuplicateSubmissionException : Exception
{
    public DuplicateSubmissionException(int groupId, string userId)
        : base($"User {userId} has already submitted a design for group {groupId}.")
    {
        GroupId = groupId;
        UserId = userId;
    }

    public int GroupId { get; }
    public string UserId { get; }
}
