using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Application.Common.Models;

namespace OjisanBackend.Infrastructure.ExternalServices;

/// <summary>
/// Implementation of IShippingService using OTO shipping API.
/// </summary>
public class OtoShippingService : IShippingService
{
    private readonly HttpClient _httpClient;
    private readonly OtoSettings _settings;
    private readonly ILogger<OtoShippingService> _logger;

    public OtoShippingService(
        IHttpClientFactory httpClientFactory,
        IOptions<OtoSettings> settings,
        ILogger<OtoShippingService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(OtoShippingService));
        _settings = settings.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            throw new InvalidOperationException("OtoSettings:BaseUrl is not configured in appsettings.json.");
        }

        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.Token}");
        _httpClient.DefaultRequestHeaders.Add("X-Retailer-Id", _settings.RetailerId);
    }

    public async Task<ShippingResultDto> GenerateLabelAsync(ShippingDetailsDto details, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.RetailerId) || string.IsNullOrWhiteSpace(_settings.Token))
        {
            throw new InvalidOperationException("OtoSettings:RetailerId and Token must be configured in appsettings.json.");
        }

        // Map city and district to OTO's required taxonomy
        // OTO requires specific city/district codes - adjust based on OTO's actual API requirements
        var otoCity = MapCityToOtoTaxonomy(details.City);
        var otoDistrict = MapDistrictToOtoTaxonomy(details.District, details.City);

        // Prepare OTO API payload
        var payload = new
        {
            recipient_name = details.RecipientName,
            recipient_phone = details.PhoneNumber,
            address_line1 = details.AddressLine1,
            address_line2 = details.AddressLine2 ?? string.Empty,
            city = otoCity,
            district = otoDistrict,
            postal_code = details.PostalCode,
            item_count = details.ItemCount,
            total_value = details.TotalValue,
            currency = "SAR",
            order_reference = details.GroupId.ToString()
        };

        var jsonContent = JsonSerializer.Serialize(payload, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        });
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Make API call to OTO
        var response = await _httpClient.PostAsync("/api/v1/shipping/labels", content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("OTO API error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
            throw new InvalidOperationException($"OTO API error: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseJson = JsonDocument.Parse(responseContent);

        var trackingNumber = responseJson.RootElement.GetProperty("trackingNumber").GetString()
            ?? throw new InvalidOperationException("OTO API did not return a tracking number.");

        var labelUrl = responseJson.RootElement.GetProperty("labelUrl").GetString()
            ?? throw new InvalidOperationException("OTO API did not return a label URL.");

        _logger.LogInformation("OTO shipping label generated successfully. Tracking: {TrackingNumber}", trackingNumber);

        return new ShippingResultDto
        {
            TrackingNumber = trackingNumber,
            ShippingLabelUrl = labelUrl
        };
    }

    /// <summary>
    /// Maps city name to OTO's required city taxonomy/code.
    /// Adjust this mapping based on OTO's actual API requirements.
    /// </summary>
    private static string MapCityToOtoTaxonomy(string city)
    {
        // OTO requires specific city codes - adjust based on OTO's documentation
        return city.ToUpperInvariant() switch
        {
            "RIYADH" => "Riyadh",
            "JEDDAH" => "Jeddah",
            "DAMMAM" => "Dammam",
            "MECCA" or "MAKKAH" => "Mecca",
            "MEDINA" or "MADINAH" => "Medina",
            _ => city // Fallback to original city name
        };
    }

    /// <summary>
    /// Maps district name to OTO's required district taxonomy/code.
    /// Adjust this mapping based on OTO's actual API requirements.
    /// </summary>
    private static string MapDistrictToOtoTaxonomy(string district, string city)
    {
        // OTO requires specific district codes - adjust based on OTO's documentation
        // This is a simplified mapping - in production, you'd have a full mapping table
        return district; // For now, return as-is. Adjust based on OTO's requirements.
    }
}
