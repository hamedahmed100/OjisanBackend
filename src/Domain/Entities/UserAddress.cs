using OjisanBackend.Domain.Common;

namespace OjisanBackend.Domain.Entities;

public class UserAddress : BaseAuditableEntity
{
    public string UserId { get; set; } = string.Empty;

    public string Street { get; set; } = string.Empty;

    public string City { get; set; } = string.Empty;

    public string District { get; set; } = string.Empty;

    public string PostalCode { get; set; } = string.Empty;

    public string PhoneNumber { get; set; } = string.Empty;
}

