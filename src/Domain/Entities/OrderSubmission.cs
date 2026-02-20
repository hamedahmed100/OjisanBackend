using OjisanBackend.Domain.Common;
using OjisanBackend.Domain.Enums;
using OjisanBackend.Domain.Events;
using OjisanBackend.Domain.Exceptions;

namespace OjisanBackend.Domain.Entities;

public class OrderSubmission : BaseAuditableEntity
{
    /// <summary>
    /// Public identifier for the submission. Use this value in APIs.
    /// </summary>
    public Guid PublicId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the Group entity. Nullable for future single orders.
    /// </summary>
    public int? GroupId { get; set; }

    /// <summary>
    /// User ID of the member who submitted the design.
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// JSON representation of the custom design (colors, materials, patterns, etc.).
    /// </summary>
    public string CustomDesignJson { get; set; } = string.Empty;

    /// <summary>
    /// Calculated price for this submission.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Current status of the submission.
    /// </summary>
    public SubmissionStatus Status { get; set; } = SubmissionStatus.Submitted;

    /// <summary>
    /// Indicates whether the submission is currently locked for editing.
    /// When true, members cannot update their design even if the status allows it.
    /// </summary>
    public bool IsEditLocked { get; private set; } = true;

    /// <summary>
    /// Optional feedback from admin when rejecting or accepting.
    /// </summary>
    public string? AdminFeedback { get; set; }

    /// <summary>
    /// Rejects the submission with admin feedback.
    /// This allows the user to edit and resubmit their design.
    /// </summary>
    /// <param name="feedback">Admin feedback explaining why the submission was rejected.</param>
    /// <exception cref="ArgumentNullException">Thrown when feedback is null or empty.</exception>
    public void Reject(string feedback)
    {
        if (string.IsNullOrWhiteSpace(feedback))
        {
            throw new ArgumentException("Feedback is required when rejecting a submission.", nameof(feedback));
        }

        Status = SubmissionStatus.Rejected;
        AdminFeedback = feedback;

        // Raise domain event for notification service
        AddDomainEvent(new SubmissionRejectedEvent(PublicId, UserId, feedback));
    }

    /// <summary>
    /// Accepts the submission, clearing any previous admin feedback.
    /// </summary>
    public void Accept()
    {
        Status = SubmissionStatus.Accepted;
        AdminFeedback = null;
    }

    /// <summary>
    /// Updates the design for a rejected submission, allowing the member to fix and resubmit.
    /// </summary>
    /// <param name="newCustomDesignJson">The updated custom design JSON.</param>
    /// <exception cref="ArgumentException">Thrown when newCustomDesignJson is null or empty.</exception>
    /// <exception cref="SubmissionNotRejectedException">Thrown when submission is not in Rejected status.</exception>
    public void UpdateDesign(string newCustomDesignJson)
    {
        if (IsEditLocked)
        {
            throw new SubmissionEditLockedException(PublicId);
        }

        if (string.IsNullOrWhiteSpace(newCustomDesignJson))
        {
            throw new ArgumentException("Custom design JSON cannot be null or empty.", nameof(newCustomDesignJson));
        }

        // Business Rule: Only rejected submissions can be updated
        if (Status != SubmissionStatus.Rejected)
        {
            throw new SubmissionNotRejectedException(
                PublicId,
                Status.ToString());
        }

        // Update the design and reset status to Submitted for admin review
        CustomDesignJson = newCustomDesignJson;
        Status = SubmissionStatus.Submitted;
        AdminFeedback = null; // Clear previous feedback
    }

    /// <summary>
    /// Unlocks the submission for editing.
    /// </summary>
    public void UnlockEdit() => IsEditLocked = false;

    /// <summary>
    /// Locks the submission from further edits.
    /// </summary>
    public void LockEdit() => IsEditLocked = true;

    /// <summary>
    /// Marks a single (non-group) order as ready for review and raises a domain event.
    /// </summary>
    public void MarkSingleOrderReadyForReview()
    {
        Status = SubmissionStatus.ReadyForReview;
        AddDomainEvent(new SingleOrderReadyForReviewEvent(Id));
    }
}

