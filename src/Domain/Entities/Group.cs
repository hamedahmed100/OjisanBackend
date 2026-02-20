using OjisanBackend.Domain.Common;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Domain.Entities;

public class Group : BaseAuditableEntity
{
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

    public bool IsLargeGroup => MaxMembers >= 10;
}

