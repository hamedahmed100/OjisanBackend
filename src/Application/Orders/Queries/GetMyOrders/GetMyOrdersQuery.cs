using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Application.Orders.Queries.GetMyOrders;

/// <summary>
/// Unified order display status for both group and single orders.
/// Flow: Submitted → ReadyForReview → Accepted → Paid → Processing → Shipping → Shipped
/// </summary>
public static class OrderDisplayStatus
{
    public const string Submitted = "Submitted";
    public const string ReadyForReview = "ReadyForReview";
    public const string Accepted = "Accepted";
    public const string Paid = "Paid";
    public const string Processing = "Processing";
    public const string Shipping = "Shipping";
    public const string Shipped = "Shipped";
    public const string Rejected = "Rejected";

    /// <summary>
    /// Maps group order or single order to unified display status.
    /// </summary>
    public static string ForOrder(OrderSubmission submission, Group? group)
    {
        if (group != null)
        {
            return group.Status switch
            {
                GroupStatus.Recruiting => Submitted,
                GroupStatus.ReadyForReview => ReadyForReview,
                GroupStatus.Accepted => Accepted,
                GroupStatus.Finalized => group.ShippedAt.HasValue ? Shipped
                    : !string.IsNullOrWhiteSpace(group.TrackingNumber) ? Shipping
                    : Processing,
                GroupStatus.Cancelled => Rejected,
                GroupStatus.Rejected => Rejected,
                _ => submission.Status.ToString()
            };
        }

        // Single order
        if (submission.Status == SubmissionStatus.Accepted)
        {
            if (submission.ShippedAt.HasValue) return Shipped;
            if (!string.IsNullOrWhiteSpace(submission.TrackingNumber)) return Shipping;
            if (submission.IsPaid) return Paid;
            return Accepted;
        }

        return submission.Status switch
        {
            SubmissionStatus.Draft => Submitted,
            SubmissionStatus.ReadyForReview => ReadyForReview,
            SubmissionStatus.Submitted => Submitted,
            SubmissionStatus.Rejected => Rejected,
            SubmissionStatus.Accepted => Accepted,
            _ => submission.Status.ToString()
        };
    }
}

public record MyOrderBadgeDto
{
    public string ImageUrl { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
}

public record MyOrderAddOnDto
{
    public Guid Id { get; init; }
    public string NameAr { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public decimal Price { get; init; }
}

public record MyOrderGroupInfoDto
{
    public Guid GroupId { get; init; }
    public string Name { get; init; } = string.Empty;
    public int MaxMembers { get; init; }
    public int MembersJoinedCount { get; init; }
    public int MembersSubmittedCount { get; init; }
    public decimal GroupTotalPrice { get; init; }
    /// <summary>Group status (0=Recruiting, 1=ReadyForReview, 2=Accepted, 3=Finalized).</summary>
    public int GroupStatus { get; init; }
}

public record MyOrderDto
{
    public Guid Id { get; init; }
    public decimal Price { get; init; }
    public string Status { get; init; } = string.Empty;
    public bool IsGroupOrder { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public string CustomDesignJson { get; init; } = string.Empty;
    public string NameBehind { get; init; } = string.Empty;
    public List<MyOrderBadgeDto> Badges { get; init; } = new();
    public List<MyOrderAddOnDto> AddOns { get; init; } = new();
    public MyOrderGroupInfoDto? GroupInfo { get; init; }
}

public record GetMyOrdersQuery : IRequest<List<MyOrderDto>>;

public class GetMyOrdersQueryHandler : IRequestHandler<GetMyOrdersQuery, List<MyOrderDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IStorageUrlResolver _urlResolver;

    public GetMyOrdersQueryHandler(IApplicationDbContext context, IUser user, IStorageUrlResolver urlResolver)
    {
        _context = context;
        _user = user;
        _urlResolver = urlResolver;
    }

    public async Task<List<MyOrderDto>> Handle(GetMyOrdersQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(_user.Id, nameof(_user.Id));

        var submissions = await _context.OrderSubmissions
            .AsNoTracking()
            .Include(s => s.Badges)
            .Include(s => s.SelectedAddOns)
                .ThenInclude(sa => sa.ProductAddOn)
            .Where(s => s.UserId == _user.Id)
            .OrderByDescending(s => s.Created)
            .ToListAsync(cancellationToken);

        var groupIds = submissions
            .Where(s => s.GroupId != null)
            .Select(s => s.GroupId!.Value)
            .Distinct()
            .ToList();

        var groups = groupIds.Count > 0
            ? await _context.Groups
                .AsNoTracking()
                .Include(g => g.Members)
                .Include(g => g.Submissions)
                .Where(g => groupIds.Contains(g.Id))
                .ToListAsync(cancellationToken)
            : new();

        var groupLookup = groups.ToDictionary(g => g.Id);

        return submissions.Select(s =>
        {
            MyOrderGroupInfoDto? groupInfo = null;
            if (s.GroupId != null && groupLookup.TryGetValue(s.GroupId.Value, out var group))
            {
                var submittedSubmissions = group.Submissions
                    .Where(sub => sub.Status != SubmissionStatus.Draft)
                    .ToList();

                groupInfo = new MyOrderGroupInfoDto
                {
                    GroupId = group.PublicId,
                    Name = group.Name,
                    MaxMembers = group.MaxMembers,
                    MembersJoinedCount = 1 + group.Members.Count,
                    MembersSubmittedCount = submittedSubmissions.Count,
                    GroupTotalPrice = submittedSubmissions.Sum(sub => sub.Price),
                    GroupStatus = (int)group.Status
                };
            }

            var displayStatus = OrderDisplayStatus.ForOrder(s, s.GroupId != null && groupLookup.TryGetValue(s.GroupId.Value, out var g) ? g : null);

            return new MyOrderDto
            {
                Id = s.PublicId,
                Price = s.Price,
                Status = displayStatus,
                IsGroupOrder = s.GroupId != null,
                CreatedAt = s.Created,
                CustomDesignJson = s.CustomDesignJson,
                NameBehind = s.NameBehind ?? string.Empty,
                Badges = s.Badges
                    .Select(b => new MyOrderBadgeDto
                    {
                        ImageUrl = _urlResolver.ToPublicUrl(b.ImageUrl),
                        Comment = b.Comment
                    })
                    .ToList(),
                AddOns = s.SelectedAddOns
                    .Where(a => a.ProductAddOn != null)
                    .Select(a => new MyOrderAddOnDto
                    {
                        Id = a.ProductAddOn.PublicId,
                        NameAr = a.ProductAddOn.NameAr,
                        NameEn = a.ProductAddOn.NameEn,
                        Price = a.ProductAddOn.Price
                    })
                    .ToList(),
                GroupInfo = groupInfo
            };
        }).ToList();
    }

}

