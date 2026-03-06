using OjisanBackend.Domain.Common;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Domain.Entities;

/// <summary>
/// Product color for jacket customization. Admins can manage via database without code changes.
/// </summary>
public class ProductColor : BaseAuditableEntity
{
    public string NameAr { get; set; } = string.Empty;

    public string NameEn { get; set; } = string.Empty;

    /// <summary>
    /// Hex color code (e.g., #BB7875).
    /// </summary>
    public string HexCode { get; set; } = string.Empty;

    /// <summary>
    /// Where this color applies: Jacket, Sleeve, or Elastic.
    /// </summary>
    public ColorType Type { get; set; }
}
