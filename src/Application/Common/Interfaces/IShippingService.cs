namespace OjisanBackend.Application.Common.Interfaces;

/// <summary>
/// Service for integrating with OTO shipping API to generate shipping labels.
/// </summary>
public interface IShippingService
{
    /// <summary>
    /// Generates a shipping label via OTO API.
    /// </summary>
    /// <param name="details">Shipping details including recipient address and order information.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Shipping result containing tracking number and label URL.</returns>
    Task<ShippingResultDto> GenerateLabelAsync(ShippingDetailsDto details, CancellationToken cancellationToken);
}

/// <summary>
/// DTO containing shipping details for OTO label generation.
/// </summary>
public record ShippingDetailsDto
{
    public Guid GroupId { get; init; }
    public string RecipientName { get; init; } = string.Empty;
    public string PhoneNumber { get; init; } = string.Empty;
    public string AddressLine1 { get; init; } = string.Empty;
    public string? AddressLine2 { get; init; }
    public string City { get; init; } = string.Empty;
    public string District { get; init; } = string.Empty;
    public string PostalCode { get; init; } = string.Empty;
    public int ItemCount { get; init; }
    public decimal TotalValue { get; init; }
}

/// <summary>
/// DTO containing the result of shipping label generation.
/// </summary>
public record ShippingResultDto
{
    public string TrackingNumber { get; init; } = string.Empty;
    public string ShippingLabelUrl { get; init; } = string.Empty;
}
