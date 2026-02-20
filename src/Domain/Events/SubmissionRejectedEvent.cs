using OjisanBackend.Domain.Common;

namespace OjisanBackend.Domain.Events;

public class SubmissionRejectedEvent : BaseEvent
{
    public SubmissionRejectedEvent(Guid submissionId, string userId, string feedback)
    {
        SubmissionId = submissionId;
        UserId = userId;
        Feedback = feedback;
        RejectedAt = DateTime.UtcNow;
    }

    public Guid SubmissionId { get; }
    public string UserId { get; }
    public string Feedback { get; }
    public DateTime RejectedAt { get; }
}
