using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Application.Common.Models;

namespace OjisanBackend.Infrastructure.Payments;

/// <summary>
/// Implementation of IPaymentService using Fatorah payment gateway.
/// </summary>
public class FatorahPaymentService : IPaymentService
{
    private readonly HttpClient _httpClient;
    private readonly FatorahSettings _settings;

    public FatorahPaymentService(IHttpClientFactory httpClientFactory, IOptions<FatorahSettings> settings)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(FatorahPaymentService));
        _settings = settings.Value;
        
        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            throw new InvalidOperationException("FatorahSettings:BaseUrl is not configured in appsettings.json.");
        }
        
        _httpClient.BaseAddress = new Uri(_settings.BaseUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.ApiKey}");
    }

    public async Task<string> CreatePaymentSessionAsync(Guid groupId, decimal amount, string merchantResourceId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
        {
            throw new InvalidOperationException("FatorahSettings:ApiKey is not configured in appsettings.json.");
        }

        // Prepare the payment request payload
        var requestPayload = new
        {
            amount = amount,
            currency = "SAR",
            merchant_resource_id = merchantResourceId,
            success_url = $"{_settings.BaseUrl}/payments/success",
            failure_url = $"{_settings.BaseUrl}/payments/failure",
            // Additional Fatorah-specific fields can be added here
        };

        var jsonContent = JsonSerializer.Serialize(requestPayload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Make the API call to Fatorah
        var response = await _httpClient.PostAsync("/api/v1/payments", content, cancellationToken);
        
        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new InvalidOperationException($"Fatorah API error: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseJson = JsonDocument.Parse(responseContent);

        // Extract the checkout URL from Fatorah's response
        // Adjust the property name based on Fatorah's actual API response structure
        var checkoutUrl = responseJson.RootElement.GetProperty("checkout_url").GetString()
            ?? throw new InvalidOperationException("Fatorah API did not return a checkout URL.");

        return checkoutUrl;
    }

    public bool ValidateWebhookSignature(string payload, string signature)
    {
        if (string.IsNullOrWhiteSpace(_settings.WebhookSecret))
        {
            throw new InvalidOperationException("FatorahSettings:WebhookSecret is not configured in appsettings.json.");
        }

        if (string.IsNullOrWhiteSpace(payload) || string.IsNullOrWhiteSpace(signature))
        {
            return false;
        }

        // Compute HMAC-SHA256 signature
        using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(_settings.WebhookSecret));
        var hashBytes = hmac.ComputeHash(Encoding.UTF8.GetBytes(payload));
        var computedSignature = Convert.ToHexString(hashBytes).ToLowerInvariant();

        // Compare signatures using constant-time comparison to prevent timing attacks
        return CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(computedSignature),
            Encoding.UTF8.GetBytes(signature.ToLowerInvariant()));
    }
}
