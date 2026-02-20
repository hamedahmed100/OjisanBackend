namespace OjisanBackend.Application.Common.Models;

/// <summary>
/// Configuration settings for Trello API integration.
/// </summary>
public class TrelloSettings
{
    /// <summary>
    /// Trello API key.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Trello API token.
    /// </summary>
    public string Token { get; set; } = string.Empty;

    /// <summary>
    /// Trello board ID where production cards are created.
    /// </summary>
    public string BoardId { get; set; } = string.Empty;

    /// <summary>
    /// Trello list ID within the board where cards are added.
    /// </summary>
    public string ListId { get; set; } = string.Empty;
}
