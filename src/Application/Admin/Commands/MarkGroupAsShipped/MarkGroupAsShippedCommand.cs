using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Application.Admin.Commands.MarkGroupAsShipped;

public record MarkGroupAsShippedCommand : IRequest
{
    public Guid GroupId { get; init; }
}

public class MarkGroupAsShippedCommandHandler : IRequestHandler<MarkGroupAsShippedCommand>
{
    private readonly IApplicationDbContext _context;

    public MarkGroupAsShippedCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(MarkGroupAsShippedCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Default(request.GroupId, nameof(request.GroupId));

        var group = await _context.Groups
            .FirstOrDefaultAsync(g => g.PublicId == request.GroupId, cancellationToken);

        if (group is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(Group), request.GroupId);
        }

        group.MarkAsShipped();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
