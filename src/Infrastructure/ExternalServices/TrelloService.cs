using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Application.Common.Models;

namespace OjisanBackend.Infrastructure.ExternalServices;

/// <summary>
/// Implementation of ITrelloService using Trello REST API.
/// </summary>
public class TrelloService : ITrelloService
{
    private readonly HttpClient _httpClient;
    private readonly TrelloSettings _settings;
    private readonly ILogger<TrelloService> _logger;

    public TrelloService(
        IHttpClientFactory httpClientFactory,
        IOptions<TrelloSettings> settings,
        ILogger<TrelloService> logger)
    {
        _httpClient = httpClientFactory.CreateClient(nameof(TrelloService));
        _settings = settings.Value;
        _logger = logger;

        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || string.IsNullOrWhiteSpace(_settings.Token))
        {
            _logger.LogWarning("TrelloSettings:ApiKey or Token is not configured. Trello integration will fail.");
        }
    }

    public async Task<string> CreateCardAsync(OrderDetailsDto details, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey) || string.IsNullOrWhiteSpace(_settings.Token))
        {
            throw new InvalidOperationException("TrelloSettings:ApiKey and Token must be configured in appsettings.json.");
        }

        if (string.IsNullOrWhiteSpace(_settings.ListId))
        {
            throw new InvalidOperationException("TrelloSettings:ListId must be configured in appsettings.json.");
        }

        // Build card name
        var cardName = $"Order #{details.GroupId} - {details.ProductName} ({details.MemberSubmissions.Count} items)";

        // Build card description with member details and badge images
        var description = new StringBuilder();
        description.AppendLine($"**Group Order Details**");
        description.AppendLine($"- Group ID: `{details.GroupId}`");
        description.AppendLine($"- Product: {details.ProductName}");
        description.AppendLine($"- Total Members: {details.MaxMembers}");
        description.AppendLine($"- Submissions: {details.MemberSubmissions.Count}");
        description.AppendLine();
        description.AppendLine("**Member Submissions:**");
        description.AppendLine();

        for (int i = 0; i < details.MemberSubmissions.Count; i++)
        {
            var submission = details.MemberSubmissions[i];
            description.AppendLine($"### Member {i + 1} (User: {submission.UserId})");
            description.AppendLine($"- Price: {submission.Price:C}");
            
            if (!string.IsNullOrWhiteSpace(submission.BadgeImageUrl))
            {
                description.AppendLine($"- Badge Image: ![Badge]({submission.BadgeImageUrl})");
            }
            
            description.AppendLine();
        }

        // Prepare Trello API payload
        var payload = new
        {
            name = cardName,
            desc = description.ToString(),
            idList = _settings.ListId,
            pos = "top"
        };

        var jsonContent = JsonSerializer.Serialize(payload);
        var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

        // Make API call to Trello
        var url = $"https://api.trello.com/1/cards?key={_settings.ApiKey}&token={_settings.Token}";
        var response = await _httpClient.PostAsync(url, content, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
            _logger.LogError("Trello API error: {StatusCode} - {ErrorContent}", response.StatusCode, errorContent);
            throw new InvalidOperationException($"Trello API error: {response.StatusCode} - {errorContent}");
        }

        var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);
        var responseJson = JsonDocument.Parse(responseContent);

        var cardId = responseJson.RootElement.GetProperty("id").GetString()
            ?? throw new InvalidOperationException("Trello API did not return a card ID.");

        _logger.LogInformation("Trello card created successfully. Card ID: {CardId}", cardId);

        return cardId;
    }
}
