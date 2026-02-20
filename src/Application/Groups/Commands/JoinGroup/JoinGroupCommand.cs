using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Application.Groups.Commands.JoinGroup;

public record JoinGroupCommand : IRequest
{
    public string InviteCode { get; init; } = string.Empty;
}

public class JoinGroupCommandHandler : IRequestHandler<JoinGroupCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IInviteCodeService _inviteCodeService;

    public JoinGroupCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IInviteCodeService inviteCodeService)
    {
        _context = context;
        _user = user;
        _inviteCodeService = inviteCodeService;
    }

    public async Task Handle(JoinGroupCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.InviteCode, nameof(request.InviteCode));
        Guard.Against.Null(_user, nameof(_user));
        Guard.Against.NullOrWhiteSpace(_user.Id, nameof(_user.Id));

        // Normalize the invite code
        var normalizedCode = request.InviteCode.StartsWith("TEAM-", StringComparison.OrdinalIgnoreCase)
            ? request.InviteCode.ToUpperInvariant()
            : $"TEAM-{request.InviteCode.ToUpperInvariant()}";

        // Decode the invite code to get the group ID
        var groupId = _inviteCodeService.DecodeInviteCode(normalizedCode);
        
        Guard.Against.Null(groupId, nameof(request.InviteCode), "Invalid invite code format.");

        // Load the group with its members (required for EF Core to track the collection)
        var group = await _context.Groups
            .Include(g => g.Members)
            .FirstOrDefaultAsync(g => g.Id == groupId.Value, cancellationToken);

        if (group is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(Group), normalizedCode);
        }

        // Validate invite code matches
        if (!string.Equals(group.InviteCode, normalizedCode, StringComparison.OrdinalIgnoreCase))
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(
                $"Group with invite code {normalizedCode} not found.");
        }

        // Delegate to the domain model to enforce business rules
        // This will throw domain exceptions if invariants are violated
        group.AddMember(_user.Id!);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
