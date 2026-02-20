using OjisanBackend.Domain.Common;

namespace OjisanBackend.Domain.Entities;

public class BadgePosition : BaseAuditableEntity
{
    /// <summary>
    /// Public identifier for the badge position. Use this value in APIs.
    /// </summary>
    public Guid PublicId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the Product entity.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Name of the badge position (e.g., "Right Sleeve", "Left Chest").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Whether this badge position is required for the product.
    /// </summary>
    public bool IsRequired { get; set; }
}
