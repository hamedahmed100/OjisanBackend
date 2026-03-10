using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;

namespace OjisanBackend.Application.Admin.Queries.GetSubmissionPreview;

/// <summary>
/// Full submission details for admin review: design (colors), badges, add-ons, and optional group context.
/// Works for both single orders and group member submissions.
/// </summary>
public record SubmissionPreviewBadgeDto
{
    public string ImageUrl { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
}

public record SubmissionPreviewAddOnDto
{
    public Guid AddOnId { get; init; }
    public string NameAr { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public decimal Price { get; init; }
}

/// <summary>
/// Group context when the submission belongs to a group (null for single orders).
/// </summary>
public record SubmissionPreviewGroupContextDto
{
    public Guid GroupId { get; init; }
    public string GroupName { get; init; } = string.Empty;
    public string InviteCode { get; init; } = string.Empty;
    public int MaxMembers { get; init; }
}

public record GetSubmissionPreviewResponse
{
    public Guid SubmissionId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public int Status { get; init; }
    public string StatusText { get; init; } = string.Empty;
    /// <summary>Design JSON: colors, materials, patterns (jacket, sleeves, etc.).</summary>
    public string CustomDesignJson { get; init; } = string.Empty;
    public string? NameBehind { get; init; }
    public decimal Price { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    /// <summary>Admin feedback if previously rejected.</summary>
    public string? AdminFeedback { get; init; }
    public List<SubmissionPreviewBadgeDto> Badges { get; init; } = new();
    public List<SubmissionPreviewAddOnDto> AddOns { get; init; } = new();
    /// <summary>Set when submission is part of a group; null for single orders.</summary>
    public SubmissionPreviewGroupContextDto? GroupContext { get; init; }
}

public record GetSubmissionPreviewQuery(Guid SubmissionId) : IRequest<GetSubmissionPreviewResponse>;

public class GetSubmissionPreviewQueryHandler : IRequestHandler<GetSubmissionPreviewQuery, GetSubmissionPreviewResponse>
{
    private readonly IApplicationDbContext _context;

    public GetSubmissionPreviewQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetSubmissionPreviewResponse> Handle(GetSubmissionPreviewQuery request, CancellationToken cancellationToken)
    {
        var submission = await _context.OrderSubmissions
            .AsNoTracking()
            .Include(s => s.Badges)
            .Include(s => s.SelectedAddOns)
                .ThenInclude(sa => sa.ProductAddOn)
            .FirstOrDefaultAsync(s => s.PublicId == request.SubmissionId, cancellationToken);

        if (submission is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(Domain.Entities.OrderSubmission), request.SubmissionId);
        }

        SubmissionPreviewGroupContextDto? groupContext = null;
        if (submission.GroupId.HasValue)
        {
            var group = await _context.Groups
                .AsNoTracking()
                .Where(g => g.Id == submission.GroupId.Value)
                .Select(g => new { g.PublicId, g.Name, g.InviteCode, g.MaxMembers })
                .FirstOrDefaultAsync(cancellationToken);
            if (group != null)
            {
                groupContext = new SubmissionPreviewGroupContextDto
                {
                    GroupId = group.PublicId,
                    GroupName = group.Name,
                    InviteCode = group.InviteCode,
                    MaxMembers = group.MaxMembers
                };
            }
        }

        return new GetSubmissionPreviewResponse
        {
            SubmissionId = submission.PublicId,
            UserId = submission.UserId,
            Status = (int)submission.Status,
            StatusText = submission.Status.ToString(),
            CustomDesignJson = submission.CustomDesignJson,
            NameBehind = submission.NameBehind,
            Price = submission.Price,
            CreatedAt = submission.Created,
            AdminFeedback = submission.AdminFeedback,
            Badges = submission.Badges
                .OrderBy(b => b.Id)
                .Select(b => new SubmissionPreviewBadgeDto
                {
                    ImageUrl = b.ImageUrl,
                    Comment = b.Comment
                }).ToList(),
            AddOns = submission.SelectedAddOns
                .Where(sa => sa.ProductAddOn != null)
                .Select(sa => new SubmissionPreviewAddOnDto
                {
                    AddOnId = sa.ProductAddOn!.PublicId,
                    NameAr = sa.ProductAddOn.NameAr,
                    NameEn = sa.ProductAddOn.NameEn,
                    Price = sa.ProductAddOn.Price
                }).ToList(),
            GroupContext = groupContext
        };
    }
}
