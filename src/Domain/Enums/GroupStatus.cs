namespace OjisanBackend.Domain.Enums;

public enum GroupStatus
{
    Recruiting = 0,
    ReadyForReview = 1,
    Accepted = 2,
    Finalized = 3,
    Cancelled = 4,
    /// <summary>
    /// All members rejected; group stays in this state until members resubmit.
    /// </summary>
    Rejected = 5
}

