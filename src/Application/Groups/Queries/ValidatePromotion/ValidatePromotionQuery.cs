using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;

namespace OjisanBackend.Application.Groups.Queries.ValidatePromotion;

/// <summary>
/// Request to validate if the uniform-colour promotion is still active for a given group size.
/// Frontend can call this when user selects more than 5 people to decide whether to show the "Special Question".
/// </summary>
public record ValidatePromotionQuery : IRequest<ValidatePromotionResult>
{
    /// <summary>
    /// Number of members (e.g. 6). Promotion must have MinGroupSize &lt;= MemberCount.
    /// </summary>
    public int MemberCount { get; init; }
}

/// <summary>
/// Whether the promotion is active and its details for display.
/// </summary>
public record ValidatePromotionResult
{
    public bool IsActive { get; init; }
    public string? PromotionName { get; init; }
    public decimal DiscountPercent { get; init; }
    public DateTime? EndDateUtc { get; init; }
}

public class ValidatePromotionQueryHandler : IRequestHandler<ValidatePromotionQuery, ValidatePromotionResult>
{
    private readonly IApplicationDbContext _context;

    public ValidatePromotionQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ValidatePromotionResult> Handle(ValidatePromotionQuery request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;

        var promotion = await _context.Promotions
            .AsNoTracking()
            .Where(p => p.IsActive
                && p.MinGroupSize <= request.MemberCount
                && p.StartDate <= now
                && p.EndDate > now)
            .OrderByDescending(p => p.DiscountPercent)
            .Select(p => new { p.PromotionName, p.DiscountPercent, p.EndDate })
            .FirstOrDefaultAsync(cancellationToken);

        if (promotion == null)
        {
            return new ValidatePromotionResult
            {
                IsActive = false,
                PromotionName = null,
                DiscountPercent = 0,
                EndDateUtc = null
            };
        }

        return new ValidatePromotionResult
        {
            IsActive = true,
            PromotionName = promotion.PromotionName,
            DiscountPercent = promotion.DiscountPercent,
            EndDateUtc = promotion.EndDate
        };
    }
}
