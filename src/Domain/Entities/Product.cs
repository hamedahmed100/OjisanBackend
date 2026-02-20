using OjisanBackend.Domain.Common;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Domain.Entities;

public class Product : BaseAuditableEntity
{
    private readonly List<ProductOption> _options = new();
    private readonly List<BadgePosition> _badgePositions = new();

    /// <summary>
    /// Public identifier for the product. Use this value in APIs.
    /// </summary>
    public Guid PublicId { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Product name (e.g., "Classic Jacket", "Premium Hoodie").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Product description.
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Base price for the product before any customization options.
    /// </summary>
    public decimal BasePrice { get; set; }

    /// <summary>
    /// Type of product (Jacket, Hoodie, Pants).
    /// </summary>
    public ProductType Type { get; set; }

    /// <summary>
    /// Whether the product is currently active and available for purchase.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Read-only collection of product customization options.
    /// Options are managed through the AddOption method.
    /// </summary>
    public IReadOnlyCollection<ProductOption> Options => _options.AsReadOnly();

    /// <summary>
    /// Read-only collection of available badge positions for this product.
    /// Positions are managed through the AddBadgePosition method.
    /// </summary>
    public IReadOnlyCollection<BadgePosition> BadgePositions => _badgePositions.AsReadOnly();

    /// <summary>
    /// Adds a customization option to the product.
    /// </summary>
    /// <param name="option">The product option to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when option is null.</exception>
    public void AddOption(ProductOption option)
    {
        if (option is null)
        {
            throw new ArgumentNullException(nameof(option));
        }

        option.ProductId = Id;
        _options.Add(option);
    }

    /// <summary>
    /// Adds a badge position to the product.
    /// </summary>
    /// <param name="position">The badge position to add.</param>
    /// <exception cref="ArgumentNullException">Thrown when position is null.</exception>
    public void AddBadgePosition(BadgePosition position)
    {
        if (position is null)
        {
            throw new ArgumentNullException(nameof(position));
        }

        position.ProductId = Id;
        _badgePositions.Add(position);
    }
}
