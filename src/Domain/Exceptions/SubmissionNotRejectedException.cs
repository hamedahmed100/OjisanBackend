namespace OjisanBackend.Domain.Exceptions;

/// <summary>
/// Exception thrown when attempting to update a submission that is not in Rejected status.
/// </summary>
public class SubmissionNotRejectedException : Exception
{
    public SubmissionNotRejectedException(Guid submissionId, string currentStatus)
        : base($"Submission {submissionId} cannot be updated because it is not in Rejected status. Current status: {currentStatus}")
    {
        SubmissionId = submissionId;
        CurrentStatus = currentStatus;
    }

    public Guid SubmissionId { get; }
    public string CurrentStatus { get; }
}
