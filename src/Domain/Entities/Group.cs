using OjisanBackend.Domain.Common;
using OjisanBackend.Domain.Enums;
using OjisanBackend.Domain.Events;
using OjisanBackend.Domain.Exceptions;

namespace OjisanBackend.Domain.Entities;

public class Group : BaseAuditableEntity
{
    private readonly List<GroupMember> _members = new();
    private readonly List<OrderSubmission> _submissions = new();

    /// <summary>
    /// Public identifier for the group. Use this value in APIs.
    /// </summary>
    public Guid PublicId { get; set; } = Guid.NewGuid();

    public string LeaderUserId { get; set; } = string.Empty;

    public int ProductId { get; set; }

    public int MaxMembers { get; set; }

    /// <summary>
    /// JSON representation of the base design (colors, materials, patterns, etc.).
    /// </summary>
    public string BaseDesignJson { get; set; } = string.Empty;

    public GroupStatus Status { get; set; } = GroupStatus.Recruiting;

    /// <summary>
    /// Unique invite code used in URLs (e.g., "TEAM-XJ92").
    /// Generated automatically when the group is created.
    /// </summary>
    public string InviteCode { get; set; } = string.Empty;

    /// <summary>
    /// Trello card ID after order is pushed to production.
    /// </summary>
    public string? TrelloCardId { get; set; }

    /// <summary>
    /// OTO tracking number for shipping.
    /// </summary>
    public string? TrackingNumber { get; set; }

    /// <summary>
    /// URL to the OTO shipping label PDF.
    /// </summary>
    public string? ShippingLabelUrl { get; set; }

    /// <summary>
    /// Read-only collection of group members.
    /// Members are managed through the AddMember method to enforce business rules.
    /// </summary>
    public IReadOnlyCollection<GroupMember> Members => _members.AsReadOnly();

    /// <summary>
    /// Read-only collection of order submissions.
    /// Submissions are managed through the AddSubmission method to enforce business rules.
    /// </summary>
    public IReadOnlyCollection<OrderSubmission> Submissions => _submissions.AsReadOnly();

    /// <summary>
    /// Determines if the group requires partial payment (50/50 split) based on the threshold.
    /// The threshold is configurable via PricingSettings to allow business rule changes without code deployment.
    /// </summary>
    /// <param name="threshold">The minimum number of members required for partial payment.</param>
    /// <returns>True if the group qualifies for 50/50 payment split, false otherwise.</returns>
    public bool RequiresPartialPayment(int threshold) => MaxMembers >= threshold;

    /// <summary>
    /// Adds a member to the group, enforcing all business invariants.
    /// This method encapsulates all domain logic for member joining.
    /// </summary>
    /// <param name="userId">The user ID of the member to add.</param>
    /// <exception cref="GroupNotAcceptingMembersException">Thrown when group status is not Recruiting.</exception>
    /// <exception cref="GroupFullException">Thrown when group has reached MaxMembers.</exception>
    /// <exception cref="MemberAlreadyInGroupException">Thrown when user is already a member.</exception>
    public void AddMember(string userId)
    {
        // Business Rule 1: Group must be in Recruiting status
        if (Status != GroupStatus.Recruiting)
        {
            throw new GroupNotAcceptingMembersException(Id);
        }

        // Business Rule 2: Group must not be full
        if (_members.Count >= MaxMembers)
        {
            throw new GroupFullException(Id, _members.Count, MaxMembers);
        }

        // Business Rule 3: User must not already be a member
        if (_members.Any(m => m.UserId == userId))
        {
            throw new MemberAlreadyInGroupException(Id, userId);
        }

        // All invariants satisfied - add the member
        var member = new GroupMember
        {
            GroupId = Id,
            UserId = userId,
            JoinedAt = DateTime.UtcNow
        };

        _members.Add(member);

        // Raise domain event
        AddDomainEvent(new MemberJoinedGroupEvent(Id, userId));
    }

    /// <summary>
    /// Adds a submission to the group, enforcing all business invariants.
    /// This method encapsulates all domain logic for submission tracking and state transitions.
    /// </summary>
    /// <param name="submission">The order submission to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when submission is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when submission does not belong to this group.</exception>
    /// <exception cref="UserNotMemberOfGroupException">Thrown when user is not a member of the group.</exception>
    /// <exception cref="DuplicateSubmissionException">Thrown when user has already submitted.</exception>
    public void AddSubmission(OrderSubmission submission)
    {
        if (submission is null)
        {
            throw new ArgumentNullException(nameof(submission));
        }

        // Ensure submission belongs to this group
        submission.GroupId ??= Id;
        if (submission.GroupId != Id)
        {
            throw new InvalidOperationException("Submission does not belong to this group.");
        }

        // Business Rule 1: User must be a member of the group (either leader or joined member)
        var isMember = LeaderUserId == submission.UserId || _members.Any(m => m.UserId == submission.UserId);
        if (!isMember)
        {
            throw new UserNotMemberOfGroupException(Id, submission.UserId);
        }

        // Business Rule 2: User must not have already submitted
        if (_submissions.Any(s => s.UserId == submission.UserId))
        {
            throw new DuplicateSubmissionException(Id, submission.UserId);
        }

        // All invariants satisfied - add the submission
        _submissions.Add(submission);

        // State Machine: Check if all members have submitted
        // Note: MaxMembers includes the leader, so we check if submissions count equals MaxMembers
        if (Status == GroupStatus.Recruiting && _submissions.Count == MaxMembers)
        {
            Status = GroupStatus.ReadyForReview;
            AddDomainEvent(new GroupReadyForReviewEvent(Id));
        }
    }

    /// <summary>
    /// Evaluates the group status based on submission states.
    /// If all submissions are accepted, the group status changes to Accepted.
    /// </summary>
    public void EvaluateGroupStatus()
    {
        // Only evaluate if group is in ReadyForReview status
        if (Status != GroupStatus.ReadyForReview)
        {
            return;
        }

        // Check if all submissions are accepted
        if (_submissions.Count > 0 && _submissions.All(s => s.Status == SubmissionStatus.Accepted))
        {
            Status = GroupStatus.Accepted;
            AddDomainEvent(new GroupAcceptedEvent(Id));
        }
    }

    /// <summary>
    /// Calculates the total price for the entire group by summing all submission prices.
    /// Each OrderSubmission already has its calculated Price saved from when the member submitted it.
    /// </summary>
    /// <returns>The sum of all submission prices.</returns>
    public decimal CalculateTotalGroupPrice() => _submissions.Sum(s => s.Price);

    /// <summary>
    /// Marks the group as paid and transitions it to Finalized status.
    /// This should be called after payment is successfully completed.
    /// </summary>
    public void MarkAsPaid()
    {
        // Only allow transition from Accepted status
        if (Status != GroupStatus.Accepted)
        {
            throw new InvalidOperationException($"Group {Id} cannot be marked as paid. Current status: {Status}. Expected status: Accepted.");
        }

        Status = GroupStatus.Finalized;
    }
}

