using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Application.Groups.Queries.GetGroupByInviteCode;

public record GroupInviteInfoDto
{
    public Guid PublicId { get; init; }
    public string Name { get; init; } = string.Empty;
    public string LeaderUserId { get; init; } = string.Empty;
    public int ProductId { get; init; }
    public int MaxMembers { get; init; }
    public int CurrentMembers { get; init; }
    public GroupStatus Status { get; init; }
    public string InviteCode { get; init; } = string.Empty;
}

public record GetGroupByInviteCodeQuery : IRequest<GroupInviteInfoDto?>
{
    public string InviteCode { get; init; } = string.Empty;
}

public class GetGroupByInviteCodeQueryHandler : IRequestHandler<GetGroupByInviteCodeQuery, GroupInviteInfoDto?>
{
    private readonly IApplicationDbContext _context;
    private readonly IInviteCodeService _inviteCodeService;

    public GetGroupByInviteCodeQueryHandler(
        IApplicationDbContext context,
        IInviteCodeService inviteCodeService)
    {
        _context = context;
        _inviteCodeService = inviteCodeService;
    }

    public async Task<GroupInviteInfoDto?> Handle(GetGroupByInviteCodeQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.InviteCode, nameof(request.InviteCode));

        // Normalize the invite code (ensure it has TEAM- prefix for comparison)
        var normalizedCode = request.InviteCode.StartsWith("TEAM-", StringComparison.OrdinalIgnoreCase)
            ? request.InviteCode.ToUpperInvariant()
            : $"TEAM-{request.InviteCode.ToUpperInvariant()}";

        // Decode the invite code to get the group ID
        var groupId = _inviteCodeService.DecodeInviteCode(normalizedCode);
        
        if (!groupId.HasValue)
        {
            return null; // Invalid invite code format
        }

        var group = await _context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId.Value, cancellationToken);

        if (group == null)
        {
            return null; // Group not found
        }

        // Validate that the group is still recruiting
        if (group.Status != GroupStatus.Recruiting)
        {
            return null; // Group is no longer accepting members
        }

        // Validate that the invite code matches (in case of hash collision or manual entry)
        if (!string.Equals(group.InviteCode, normalizedCode, StringComparison.OrdinalIgnoreCase))
        {
            return null; // Invite code mismatch
        }

        // Get current member count (including the leader)
        var currentMembers = group.Members.Count + 1; // +1 for the leader

        return new GroupInviteInfoDto
        {
            PublicId = group.PublicId,
            Name = group.Name,
            LeaderUserId = group.LeaderUserId,
            ProductId = group.ProductId,
            MaxMembers = group.MaxMembers,
            CurrentMembers = currentMembers,
            Status = group.Status,
            InviteCode = group.InviteCode
        };
    }
}
