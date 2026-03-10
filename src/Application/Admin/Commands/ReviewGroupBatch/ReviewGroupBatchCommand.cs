using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Application.Admin.Commands.ReviewGroupBatch;

public record ReviewDecisionDto
{
    public Guid SubmissionId { get; init; }
    public bool IsApproved { get; init; }
    public string? Feedback { get; init; }
}

public record ReviewGroupBatchCommand : IRequest
{
    public Guid GroupId { get; init; }
    public List<ReviewDecisionDto> Decisions { get; init; } = new();
}

public class ReviewGroupBatchCommandHandler : IRequestHandler<ReviewGroupBatchCommand>
{
    private readonly IApplicationDbContext _context;

    public ReviewGroupBatchCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ReviewGroupBatchCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Default(request.GroupId, nameof(request.GroupId));
        Guard.Against.NullOrEmpty(request.Decisions, nameof(request.Decisions));

        var group = await _context.Groups
            .Include(g => g.Submissions)
            .FirstOrDefaultAsync(g => g.PublicId == request.GroupId, cancellationToken);

        if (group is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(Group), request.GroupId);
        }

        if (group.Status != Domain.Enums.GroupStatus.ReadyForReview)
        {
            throw new InvalidOperationException(
                $"Group must be in ReadyForReview status to batch review. Current status: {group.Status}.");
        }

        var submissionIds = group.Submissions.Select(s => s.PublicId).ToHashSet();
        var decisionIds = request.Decisions.Select(d => d.SubmissionId).ToHashSet();

        if (!submissionIds.SetEquals(decisionIds))
        {
            var missing = submissionIds.Except(decisionIds).ToList();
            var extra = decisionIds.Except(submissionIds).ToList();
            var errors = new List<string>();
            if (missing.Any())
                errors.Add($"Missing decisions for submissions: {string.Join(", ", missing)}");
            if (extra.Any())
                errors.Add($"Decisions for submissions not in group: {string.Join(", ", extra)}");
            throw new InvalidOperationException(string.Join("; ", errors));
        }

        foreach (var decision in request.Decisions)
        {
            var submission = group.Submissions.First(s => s.PublicId == decision.SubmissionId);

            if (decision.IsApproved)
            {
                submission.Accept();
            }
            else
            {
                if (string.IsNullOrWhiteSpace(decision.Feedback))
                {
                    throw new InvalidOperationException(
                        $"Feedback is required when rejecting submission {decision.SubmissionId}.");
                }
                submission.Reject(decision.Feedback);
            }
        }

        group.EvaluateGroupStatus();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
