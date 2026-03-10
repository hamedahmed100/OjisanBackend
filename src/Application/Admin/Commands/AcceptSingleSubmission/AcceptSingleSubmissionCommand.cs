using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Application.Admin.Commands.AcceptSingleSubmission;

public record AcceptSingleSubmissionCommand : IRequest
{
    public Guid SubmissionId { get; init; }
}

public class AcceptSingleSubmissionCommandHandler : IRequestHandler<AcceptSingleSubmissionCommand>
{
    private readonly IApplicationDbContext _context;

    public AcceptSingleSubmissionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(AcceptSingleSubmissionCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Default(request.SubmissionId, nameof(request.SubmissionId));

        var submission = await _context.OrderSubmissions
            .FirstOrDefaultAsync(s => s.PublicId == request.SubmissionId, cancellationToken);

        if (submission is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(OrderSubmission), request.SubmissionId);
        }

        if (submission.GroupId != null)
        {
            throw new InvalidOperationException("Use group review endpoint for group submissions.");
        }

        if (submission.Status != SubmissionStatus.ReadyForReview)
        {
            throw new InvalidOperationException(
                $"Submission must be in ReadyForReview status to accept. Current status: {submission.Status}.");
        }

        submission.Accept();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
