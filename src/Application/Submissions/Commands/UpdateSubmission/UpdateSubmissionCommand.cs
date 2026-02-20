using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;
using OjisanBackend.Domain.Exceptions;

namespace OjisanBackend.Application.Submissions.Commands.UpdateSubmission;

public record UpdateSubmissionCommand : IRequest
{
    public Guid GroupId { get; init; }

    public Guid SubmissionId { get; init; }

    public string NewCustomDesignJson { get; init; } = string.Empty;
}

public class UpdateSubmissionCommandHandler : IRequestHandler<UpdateSubmissionCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public UpdateSubmissionCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(UpdateSubmissionCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Default(request.GroupId, nameof(request.GroupId));
        Guard.Against.Default(request.SubmissionId, nameof(request.SubmissionId));
        Guard.Against.NullOrWhiteSpace(request.NewCustomDesignJson, nameof(request.NewCustomDesignJson));
        Guard.Against.Null(_user, nameof(_user));
        Guard.Against.NullOrWhiteSpace(_user.Id, nameof(_user.Id));

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

        // Verify the current user owns the submission
        if (submission.UserId != _user.Id)
        {
            throw new ForbiddenAccessException("You can only update your own submissions.");
        }

        // Delegate to the domain model to enforce business rules
        // This will throw SubmissionNotRejectedException if the submission is not in Rejected status
        submission.UpdateDesign(request.NewCustomDesignJson);

        // Re-evaluate group status in case this was the last rejected item holding the group back
        // (e.g., if all other submissions were accepted and this was the only rejected one)
        group.EvaluateGroupStatus();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
