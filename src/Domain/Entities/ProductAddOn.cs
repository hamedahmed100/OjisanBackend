using OjisanBackend.Domain.Common;

namespace OjisanBackend.Domain.Entities;

/// <summary>
/// Paid add-on for a product (e.g., Fixed Hood, Full Leather). Admins can manage via database.
/// </summary>
public class ProductAddOn : BaseAuditableEntity
{
    /// <summary>
    /// Public identifier for the add-on. Use this value in APIs.
    /// </summary>
    public Guid PublicId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Arabic name (e.g., "قبعة ثابتة").
    /// </summary>
    public string NameAr { get; set; } = string.Empty;

    /// <summary>
    /// English name (e.g., "Fixed Hood").
    /// </summary>
    public string NameEn { get; set; } = string.Empty;

    /// <summary>
    /// Price in SAR.
    /// </summary>
    public decimal Price { get; set; }

    /// <summary>
    /// Optional: Product this add-on belongs to. Null = available for all jacket products.
    /// </summary>
    public int? ProductId { get; set; }

    public Product? Product { get; set; }
}
