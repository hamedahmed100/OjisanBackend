using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Application.Admin.Queries.GetGroupPreview;

public record GroupPreviewMemberBadgeDto
{
    public string ImageUrl { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
}

public record GroupPreviewMemberAddOnDto
{
    public Guid AddOnId { get; init; }
    public string NameAr { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public decimal Price { get; init; }
}

public record GroupPreviewMemberDto
{
    public Guid SubmissionId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public int Status { get; init; }
    public string StatusText { get; init; } = string.Empty;
    public string CustomDesignJson { get; init; } = string.Empty;
    public string? NameBehind { get; init; }
    public decimal Price { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string? AdminFeedback { get; init; }
    public List<GroupPreviewMemberBadgeDto> Badges { get; init; } = new();
    public List<GroupPreviewMemberAddOnDto> AddOns { get; init; } = new();
}

public record GetGroupPreviewResponse
{
    public Guid GroupId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string LeaderUserId { get; init; } = string.Empty;
    public string InviteCode { get; init; } = string.Empty;
    public int MaxMembers { get; init; }
    public int CurrentSubmissions { get; init; }
    public bool IsUniformColorSelected { get; init; }
    public string BaseDesignJson { get; init; } = string.Empty;
    public string? NameBehind { get; init; }
    public int Status { get; init; }
    public string StatusText { get; init; } = string.Empty;
    public decimal TotalPrice { get; init; }
    public List<GroupPreviewMemberDto> Members { get; init; } = new();
}

public record GetGroupPreviewQuery(Guid GroupId) : IRequest<GetGroupPreviewResponse>;

public class GetGroupPreviewQueryHandler : IRequestHandler<GetGroupPreviewQuery, GetGroupPreviewResponse>
{
    private readonly IApplicationDbContext _context;

    public GetGroupPreviewQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<GetGroupPreviewResponse> Handle(GetGroupPreviewQuery request, CancellationToken cancellationToken)
    {
        var group = await _context.Groups
            .AsNoTracking()
            .Include(g => g.Submissions)
                .ThenInclude(s => s.Badges)
            .Include(g => g.Submissions)
                .ThenInclude(s => s.SelectedAddOns)
                    .ThenInclude(sa => sa.ProductAddOn)
            .FirstOrDefaultAsync(g => g.PublicId == request.GroupId, cancellationToken);

        if (group is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(Domain.Entities.Group), request.GroupId);
        }

        var members = group.Submissions
            .OrderBy(s => s.Created)
            .Select(s => new GroupPreviewMemberDto
            {
                SubmissionId = s.PublicId,
                UserId = s.UserId,
                Status = (int)s.Status,
                StatusText = s.Status.ToString(),
                CustomDesignJson = s.CustomDesignJson,
                NameBehind = s.NameBehind,
                Price = s.Price,
                CreatedAt = s.Created,
                AdminFeedback = s.AdminFeedback,
                Badges = s.Badges
                    .OrderBy(b => b.Id)
                    .Select(b => new GroupPreviewMemberBadgeDto
                    {
                        ImageUrl = b.ImageUrl,
                        Comment = b.Comment
                    }).ToList(),
                AddOns = s.SelectedAddOns
                    .Where(sa => sa.ProductAddOn != null)
                    .Select(sa => new GroupPreviewMemberAddOnDto
                    {
                        AddOnId = sa.ProductAddOn!.PublicId,
                        NameAr = sa.ProductAddOn.NameAr,
                        NameEn = sa.ProductAddOn.NameEn,
                        Price = sa.ProductAddOn.Price
                    }).ToList()
            }).ToList();

        var totalPrice = members.Sum(m => m.Price);

        return new GetGroupPreviewResponse
        {
            GroupId = group.PublicId,
            Name = group.Name,
            LeaderUserId = group.LeaderUserId,
            InviteCode = group.InviteCode,
            MaxMembers = group.MaxMembers,
            CurrentSubmissions = group.Submissions.Count(s => s.Status != SubmissionStatus.Draft),
            IsUniformColorSelected = group.IsUniformColorSelected,
            BaseDesignJson = group.BaseDesignJson,
            NameBehind = group.NameBehind,
            Status = (int)group.Status,
            StatusText = group.Status.ToString(),
            TotalPrice = totalPrice,
            Members = members
        };
    }
}

