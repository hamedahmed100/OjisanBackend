using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Application.Admin.Commands.RejectSingleSubmission;

public record RejectSingleSubmissionCommand : IRequest
{
    public Guid SubmissionId { get; init; }
    public string Feedback { get; init; } = string.Empty;
}

public class RejectSingleSubmissionCommandHandler : IRequestHandler<RejectSingleSubmissionCommand>
{
    private readonly IApplicationDbContext _context;

    public RejectSingleSubmissionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(RejectSingleSubmissionCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Default(request.SubmissionId, nameof(request.SubmissionId));
        Guard.Against.NullOrWhiteSpace(request.Feedback, nameof(request.Feedback), "Feedback is required when rejecting a submission.");

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
                $"Submission must be in ReadyForReview status to reject. Current status: {submission.Status}.");
        }

        submission.Reject(request.Feedback);

        await _context.SaveChangesAsync(cancellationToken);
    }
}
