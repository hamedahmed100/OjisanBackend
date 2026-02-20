using OjisanBackend.Domain.Common;

namespace OjisanBackend.Domain.Entities;

public class GroupMember : BaseAuditableEntity
{
    /// <summary>
    /// Public identifier for the group member record.
    /// </summary>
    public Guid PublicId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the Group entity.
    /// </summary>
    public int GroupId { get; set; }

    /// <summary>
    /// User ID of the member (from Identity).
    /// </summary>
    public string UserId { get; set; } = string.Empty;

    /// <summary>
    /// Timestamp when the member joined the group.
    /// </summary>
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the Group (not exposed publicly to maintain aggregate boundaries).
    /// </summary>
    internal Group? Group { get; set; }
}
