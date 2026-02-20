using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Application.Common.Models;

namespace OjisanBackend.Infrastructure.Notifications;

/// <summary>
/// Implementation of IWhatsAppService using Twilio/360Dialog API.
/// </summary>
public class WhatsAppService : IWhatsAppService
{
    private readonly HttpClient _httpClient;
    private readonly WhatsAppSettings _settings;
    private readonly ILogger<WhatsAppService> _logger;

    public WhatsAppService(
        IHttpClientFactory httpClientFactory,
        IOptions<WhatsAppSettings> settings,
        ILogger<WhatsAppService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(WhatsAppService));
        _settings = settings.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_settings.ApiUrl))
        {
            throw new InvalidOperationException("WhatsAppSettings:ApiUrl is not configured in appsettings.json.");
        }

        _httpClient.BaseAddress = new Uri(_settings.ApiUrl);
        _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {_settings.Token}");
    }

    public async Task SendMessageAsync(string toPhoneNumber, string templateName, Dictionary<string, string> parameters, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.Token))
        {
            throw new InvalidOperationException("WhatsAppSettings:Token is not configured in appsettings.json.");
        }

        if (string.IsNullOrWhiteSpace(toPhoneNumber))
        {
            throw new ArgumentException("Recipient phone number cannot be empty.", nameof(toPhoneNumber));
        }

        // Format phone number for KSA (ensure +966 prefix)
        var formattedPhone = FormatPhoneNumberForKSA(toPhoneNumber);

        try
        {
            // Prepare WhatsApp API payload
            // Adjust the structure based on your WhatsApp provider's API (Twilio/360Dialog)
            var payload = new
            {
                to = formattedPhone,
                from = _settings.SenderNumber,
                template = new
                {
                    name = templateName,
                    language = new { code = "ar" }, // Arabic by default for KSA market
                    components = parameters.Select(p => new
                    {
                        type = "text",
                        text = p.Value
                    }).ToArray()
                }
            };

            var jsonContent = JsonSerializer.Serialize(payload, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            // Make API call to WhatsApp provider
            // Adjust endpoint based on provider (e.g., "/v1/Messages" for Twilio, "/v1/messages" for 360Dialog)
            var response = await _httpClient.PostAsync("/v1/messages", content, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                _logger.LogError("WhatsApp API error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
                throw new InvalidOperationException($"WhatsApp API error: {response.StatusCode} - {errorContent}");
            }

            _logger.LogInformation("WhatsApp message sent successfully to {PhoneNumber} using template {TemplateName}", formattedPhone, templateName);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send WhatsApp message to {PhoneNumber}. Template: {TemplateName}", formattedPhone, templateName);
            throw;
        }
    }

    /// <summary>
    /// Formats phone number to ensure it has the KSA country code (+966).
    /// </summary>
    private static string FormatPhoneNumberForKSA(string phoneNumber)
    {
        if (string.IsNullOrWhiteSpace(phoneNumber))
        {
            return phoneNumber;
        }

        // Remove any whitespace or special characters except +
        var cleaned = Regex.Replace(phoneNumber, @"[^\d+]", "");

        // If it starts with 0, replace with +966
        if (cleaned.StartsWith("0"))
        {
            return "+966" + cleaned.Substring(1);
        }

        // If it starts with 966, add +
        if (cleaned.StartsWith("966"))
        {
            return "+" + cleaned;
        }

        // If it already has +966, return as-is
        if (cleaned.StartsWith("+966"))
        {
            return cleaned;
        }

        // If it's a local number (10 digits starting with 5), add +966
        if (cleaned.Length == 10 && cleaned.StartsWith("5"))
        {
            return "+966" + cleaned;
        }

        // Otherwise, assume it's already formatted correctly
        return cleaned;
    }
}
