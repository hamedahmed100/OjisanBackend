using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Application.Groups.Queries.GetMyGroups;

public record MyGroupDto
{
    public Guid GroupId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Role { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public int MaxMembers { get; init; }
    public int MembersJoinedCount { get; init; }
    public int MembersSubmittedCount { get; init; }
    public bool IsUniformColorSelected { get; init; }
    public string InviteCode { get; init; } = string.Empty;
    public DateTimeOffset CreatedAt { get; init; }
}

public record GetMyGroupsQuery : IRequest<List<MyGroupDto>>;

public class GetMyGroupsQueryHandler : IRequestHandler<GetMyGroupsQuery, List<MyGroupDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public GetMyGroupsQueryHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<List<MyGroupDto>> Handle(GetMyGroupsQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(_user.Id, nameof(_user.Id));

        var groups = await _context.Groups
            .AsNoTracking()
            .Include(g => g.Members)
            .Include(g => g.Submissions)
            .Where(g => g.LeaderUserId == _user.Id
                || g.Members.Any(m => m.UserId == _user.Id))
            .OrderByDescending(g => g.Created)
            .ToListAsync(cancellationToken);

        return groups.Select(g => new MyGroupDto
        {
            GroupId = g.PublicId,
            Name = g.Name,
            Role = g.LeaderUserId == _user.Id ? "Leader" : "Member",
            Status = g.Status.ToString(),
            MaxMembers = g.MaxMembers,
            MembersJoinedCount = 1 + g.Members.Count,
            MembersSubmittedCount = g.Submissions.Count(s => s.Status != SubmissionStatus.Draft),
            IsUniformColorSelected = g.IsUniformColorSelected,
            InviteCode = g.InviteCode,
            CreatedAt = g.Created
        }).ToList();
    }
}
