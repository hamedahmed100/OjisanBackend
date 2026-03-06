using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;

namespace OjisanBackend.Application.Orders.Queries.GetMyOrders;

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

    public GetMyOrdersQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
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
                    .Where(sub => sub.Status != Domain.Enums.SubmissionStatus.Draft)
                    .ToList();

                groupInfo = new MyOrderGroupInfoDto
                {
                    GroupId = group.PublicId,
                    Name = group.Name,
                    MaxMembers = group.MaxMembers,
                    MembersJoinedCount = 1 + group.Members.Count,
                    MembersSubmittedCount = submittedSubmissions.Count,
                    GroupTotalPrice = submittedSubmissions.Sum(sub => sub.Price)
                };
            }

            return new MyOrderDto
            {
                Id = s.PublicId,
                Price = s.Price,
                Status = s.Status.ToString(),
                IsGroupOrder = s.GroupId != null,
                CreatedAt = s.Created,
                CustomDesignJson = s.CustomDesignJson,
                NameBehind = s.NameBehind ?? string.Empty,
                Badges = s.Badges
                    .Select(b => new MyOrderBadgeDto
                    {
                        ImageUrl = ToRelativeImagePath(b.ImageUrl),
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

    private static string ToRelativeImagePath(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            return string.Empty;
        }

        const string marker = "/uploads/badges/";
        var index = imageUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);

        return index >= 0
            ? imageUrl[index..]
            : imageUrl;
    }
}

