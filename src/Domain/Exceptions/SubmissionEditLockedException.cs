namespace OjisanBackend.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to edit a submission while it is locked.
/// </summary>
public class SubmissionEditLockedException : Exception
{
    public SubmissionEditLockedException(Guid submissionId)
        : base($"Submission {submissionId} cannot be edited because it is locked.")
    {
        SubmissionId = submissionId;
    }

    public Guid SubmissionId { get; }
}

