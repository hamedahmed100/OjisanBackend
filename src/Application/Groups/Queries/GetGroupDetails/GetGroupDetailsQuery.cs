using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Application.Groups.Queries.GetGroupDetails;

public record GroupMemberDto
{
    public string UserId { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public bool IsLeader { get; init; }
    public DateTimeOffset JoinedAt { get; init; }
    public bool HasSubmitted { get; init; }
}

public record GroupSubmissionBadgeDto
{
    public string ImageUrl { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
}

public record GroupSubmissionAddOnDto
{
    public Guid Id { get; init; }
    public string NameAr { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public decimal Price { get; init; }
}

public record GroupSubmissionDto
{
    public Guid SubmissionId { get; init; }
    public string UserId { get; init; } = string.Empty;
    public string? DisplayName { get; init; }
    public bool IsLeader { get; init; }
    public string Status { get; init; } = string.Empty;
    public string CustomDesignJson { get; init; } = string.Empty;
    public string NameBehind { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public List<GroupSubmissionBadgeDto> Badges { get; init; } = new();
    public List<GroupSubmissionAddOnDto> AddOns { get; init; } = new();
}

public record GetGroupDetailsResult
{
    public Guid GroupId { get; init; }
    public string Name { get; init; } = string.Empty;
    public GroupStatus Status { get; init; }
    public int MaxMembers { get; init; }
    public int MembersJoinedCount { get; init; }
    public int MembersSubmittedCount { get; init; }
    public string InviteCode { get; init; } = string.Empty;
    public string InviteLink { get; init; } = string.Empty;
    public bool IsUniformColorSelected { get; init; }
    public string BaseDesignJson { get; init; } = string.Empty;
    public List<GroupMemberDto> Members { get; init; } = new();
    public List<GroupSubmissionDto> Submissions { get; init; } = new();
}

public record GetGroupDetailsQuery : IRequest<GetGroupDetailsResult>
{
    public Guid GroupId { get; init; }
}

public class GetGroupDetailsQueryHandler : IRequestHandler<GetGroupDetailsQuery, GetGroupDetailsResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IIdentityService _identityService;
    private readonly IConfiguration _configuration;

    public GetGroupDetailsQueryHandler(
        IApplicationDbContext context,
        IUser user,
        IIdentityService identityService,
        IConfiguration configuration)
    {
        _context = context;
        _user = user;
        _identityService = identityService;
        _configuration = configuration;
    }

    public async Task<GetGroupDetailsResult> Handle(GetGroupDetailsQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Default(request.GroupId, nameof(request.GroupId));
        Guard.Against.NullOrWhiteSpace(_user.Id, nameof(_user.Id));

        var group = await _context.Groups
            .AsNoTracking()
            .Include(g => g.Members)
            .Include(g => g.Submissions)
                .ThenInclude(s => s.Badges)
            .Include(g => g.Submissions)
                .ThenInclude(s => s.SelectedAddOns)
                    .ThenInclude(sa => sa.ProductAddOn)
            .FirstOrDefaultAsync(g => g.PublicId == request.GroupId, cancellationToken);

        if (group == null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(Domain.Entities.Group), request.GroupId);
        }

        var isLeader = group.LeaderUserId == _user.Id;
        var isMember = isLeader || group.Members.Any(m => m.UserId == _user.Id);
        if (!isMember)
        {
            throw new ForbiddenAccessException("You are not a member of this group.");
        }

        var membersJoinedCount = 1 + group.Members.Count;
        var membersSubmittedCount = group.Submissions.Count;

        var frontendBaseUrl = (_configuration["FrontendBaseUrl"] ?? "http://localhost:4200").TrimEnd('/');
        var inviteLink = $"{frontendBaseUrl}/join/{group.InviteCode}";

        var memberUserIds = new[] { group.LeaderUserId }
            .Concat(group.Members.Select(m => m.UserId))
            .Distinct()
            .ToList();

        var displayNames = new Dictionary<string, string?>();
        foreach (var uid in memberUserIds)
        {
            displayNames[uid] = await _identityService.GetUserNameAsync(uid);
        }

        var submittedUserIds = group.Submissions.Select(s => s.UserId).ToHashSet();

        var members = new List<GroupMemberDto>
        {
            new GroupMemberDto
            {
                UserId = group.LeaderUserId,
                DisplayName = displayNames.GetValueOrDefault(group.LeaderUserId),
                IsLeader = true,
                JoinedAt = group.Created,
                HasSubmitted = submittedUserIds.Contains(group.LeaderUserId)
            }
        };

        foreach (var m in group.Members.OrderBy(m => m.JoinedAt))
        {
            members.Add(new GroupMemberDto
            {
                UserId = m.UserId,
                DisplayName = displayNames.GetValueOrDefault(m.UserId),
                IsLeader = false,
                JoinedAt = new DateTimeOffset(m.JoinedAt, TimeSpan.Zero),
                HasSubmitted = submittedUserIds.Contains(m.UserId)
            });
        }

        var submissions = group.Submissions
            .Select(s => new GroupSubmissionDto
            {
                SubmissionId = s.PublicId,
                UserId = s.UserId,
                DisplayName = displayNames.GetValueOrDefault(s.UserId),
                IsLeader = s.UserId == group.LeaderUserId,
                Status = s.Status.ToString(),
                CustomDesignJson = s.CustomDesignJson,
                NameBehind = s.NameBehind ?? string.Empty,
                Price = s.Price,
                Badges = s.Badges
                    .Select(b => new GroupSubmissionBadgeDto
                    {
                        ImageUrl = ToRelativeImagePath(b.ImageUrl),
                        Comment = b.Comment
                    })
                    .ToList(),
                AddOns = s.SelectedAddOns
                    .Where(sa => sa.ProductAddOn != null)
                    .Select(sa => new GroupSubmissionAddOnDto
                    {
                        Id = sa.ProductAddOn!.PublicId,
                        NameAr = sa.ProductAddOn.NameAr,
                        NameEn = sa.ProductAddOn.NameEn,
                        Price = sa.ProductAddOn.Price
                    })
                    .ToList()
            })
            .OrderBy(s => s.IsLeader ? 0 : 1)
            .ThenBy(s => s.UserId)
            .ToList();

        return new GetGroupDetailsResult
        {
            GroupId = group.PublicId,
            Name = group.Name,
            Status = group.Status,
            MaxMembers = group.MaxMembers,
            MembersJoinedCount = membersJoinedCount,
            MembersSubmittedCount = membersSubmittedCount,
            InviteCode = group.InviteCode,
            InviteLink = inviteLink,
            IsUniformColorSelected = group.IsUniformColorSelected,
            BaseDesignJson = group.BaseDesignJson,
            Members = members,
            Submissions = submissions
        };
    }

    private static string ToRelativeImagePath(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return string.Empty;
        const string marker = "/uploads/badges/";
        var index = imageUrl.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        return index >= 0 ? imageUrl[index..] : imageUrl;
    }
}
