using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Application.Admin.Commands.ReviewSubmission;

public record ReviewSubmissionCommand : IRequest
{
    public Guid GroupId { get; init; }

    public Guid SubmissionId { get; init; }

    public bool IsApproved { get; init; }

    public string? Feedback { get; init; }
}

public class ReviewSubmissionCommandHandler : IRequestHandler<ReviewSubmissionCommand>
{
    private readonly IApplicationDbContext _context;

    public ReviewSubmissionCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ReviewSubmissionCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Default(request.GroupId, nameof(request.GroupId));
        Guard.Against.Default(request.SubmissionId, nameof(request.SubmissionId));

        // Fetch the group with its submissions
        var group = await _context.Groups
            .Include(g => g.Submissions)
            .FirstOrDefaultAsync(g => g.PublicId == request.GroupId, cancellationToken);

        if (group is null)
        {
            throw new NotFoundException(nameof(Group), request.GroupId);
        }

        // Locate the specific submission within the group
        var submission = group.Submissions.FirstOrDefault(s => s.PublicId == request.SubmissionId);
        
        if (submission is null)
        {
            throw new NotFoundException(nameof(OrderSubmission), request.SubmissionId);
        }

        // Apply the review decision
        if (request.IsApproved)
        {
            submission.Accept();
        }
        else
        {
            Guard.Against.NullOrWhiteSpace(request.Feedback, nameof(request.Feedback), "Feedback is required when rejecting a submission.");
            submission.Reject(request.Feedback!);
        }

        // Evaluate if the group status should change (e.g., all submissions accepted)
        group.EvaluateGroupStatus();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
