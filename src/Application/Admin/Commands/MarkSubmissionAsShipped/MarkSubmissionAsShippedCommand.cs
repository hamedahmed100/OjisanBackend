using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Application.Admin.Commands.MarkSubmissionAsShipped;

public record MarkSubmissionAsShippedCommand : IRequest
{
    public Guid SubmissionId { get; init; }
}

public class MarkSubmissionAsShippedCommandHandler : IRequestHandler<MarkSubmissionAsShippedCommand>
{
    private readonly IApplicationDbContext _context;

    public MarkSubmissionAsShippedCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(MarkSubmissionAsShippedCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Default(request.SubmissionId, nameof(request.SubmissionId));

        var submission = await _context.OrderSubmissions
            .FirstOrDefaultAsync(s => s.PublicId == request.SubmissionId, cancellationToken);

        if (submission is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(OrderSubmission), request.SubmissionId);
        }

        submission.MarkAsShipped();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
