using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Application.Admin.Queries.GetGroupsInReview;

public record GroupInReviewDto
{
    public Guid GroupId { get; init; }
    public string LeaderUserId { get; init; } = string.Empty;
    public int ProductId { get; init; }
    public int MaxMembers { get; init; }
    public int CurrentSubmissions { get; init; }
    public string InviteCode { get; init; } = string.Empty;
    public List<SubmissionSummaryDto> Submissions { get; init; } = new();
}

public record SubmissionSummaryDto
{
    public Guid SubmissionId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public SubmissionStatus Status { get; init; }
    public string? BadgeImageUrl { get; init; }
}

public record GetGroupsInReviewQuery : IRequest<List<GroupInReviewDto>>;

public class GetGroupsInReviewQueryHandler : IRequestHandler<GetGroupsInReviewQuery, List<GroupInReviewDto>>
{
    private readonly IApplicationDbContext _context;

    public GetGroupsInReviewQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<GroupInReviewDto>> Handle(GetGroupsInReviewQuery request, CancellationToken cancellationToken)
    {
        // First, fetch the groups with submissions from the database (materialize the query)
        // This ensures EF Core doesn't try to translate ExtractBadgeImageUrl to SQL
        var groupsData = await _context.Groups
            .AsNoTracking()
            .Include(g => g.Submissions)
            .Where(g => g.Status == GroupStatus.ReadyForReview)
            .ToListAsync(cancellationToken);

        // Now map the results in-memory where ExtractBadgeImageUrl can run safely
        var groups = groupsData.Select(g => new GroupInReviewDto
        {
            GroupId = g.PublicId,
            LeaderUserId = g.LeaderUserId,
            ProductId = g.ProductId,
            MaxMembers = g.MaxMembers,
            CurrentSubmissions = g.Submissions.Count,
            InviteCode = g.InviteCode,
            Submissions = g.Submissions.Select(s => new SubmissionSummaryDto
            {
                SubmissionId = s.PublicId,
                UserId = s.UserId,
                Price = s.Price,
                Status = s.Status,
                BadgeImageUrl = ExtractBadgeImageUrl(s.CustomDesignJson)
            }).ToList()
        }).ToList();

        return groups;
    }

    private static string? ExtractBadgeImageUrl(string customDesignJson)
    {
        // Simple extraction - in production, you might want to parse the JSON properly
        // This assumes the JSON contains a "badgeImageUrl" or similar field
        if (string.IsNullOrWhiteSpace(customDesignJson))
        {
            return null;
        }

        // Try to extract badge image URL from JSON
        // This is a simplified version - you may want to use System.Text.Json to parse properly
        var urlMatch = System.Text.RegularExpressions.Regex.Match(
            customDesignJson,
            @"""badgeImageUrl""\s*:\s*""([^""]+)""",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return urlMatch.Success ? urlMatch.Groups[1].Value : null;
    }
}
