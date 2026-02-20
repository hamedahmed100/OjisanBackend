using OjisanBackend.Domain.Common;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Domain.Entities;

public class ProductOption : BaseAuditableEntity
{
    /// <summary>
    /// Public identifier for the product option. Use this value in APIs.
    /// </summary>
    public Guid PublicId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Foreign key to the Product entity.
    /// </summary>
    public int ProductId { get; set; }

    /// <summary>
    /// Category of the option (BaseColor, SleeveMaterial, SleevePattern, AddOn).
    /// </summary>
    public OptionCategory Category { get; set; }

    /// <summary>
    /// Value of the option (e.g., "Red", "Leather", "Striped").
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Additional cost for selecting this option.
    /// </summary>
    public decimal AdditionalCost { get; set; }
}
