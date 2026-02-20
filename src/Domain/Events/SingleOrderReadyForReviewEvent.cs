using OjisanBackend.Domain.Common;

namespace OjisanBackend.Domain.Events;

public class SingleOrderReadyForReviewEvent : BaseEvent
{
    public SingleOrderReadyForReviewEvent(int submissionId)
    {
        SubmissionId = submissionId;
        ReadyAt = DateTime.UtcNow;
    }

    public int SubmissionId { get; }
    public DateTime ReadyAt { get; }
}

