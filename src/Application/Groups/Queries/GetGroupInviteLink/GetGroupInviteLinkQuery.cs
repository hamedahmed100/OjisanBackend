using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Application.Groups.Queries.GetGroupInviteLink;

public record GetGroupInviteLinkQuery : IRequest<string>
{
    public Guid GroupId { get; init; }
}

public class GetGroupInviteLinkQueryHandler : IRequestHandler<GetGroupInviteLinkQuery, string>
{
    private readonly IApplicationDbContext _context;

    public GetGroupInviteLinkQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<string> Handle(GetGroupInviteLinkQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Default(request.GroupId, nameof(request.GroupId));

        var group = await _context.Groups
            .FirstOrDefaultAsync(g => g.PublicId == request.GroupId, cancellationToken);

        if (group is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(
                nameof(Group), request.GroupId);
        }

        return group.InviteCode;
    }
}
