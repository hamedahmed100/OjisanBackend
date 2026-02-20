using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Application.Submissions.Commands.SubmitMemberDesign;

public record SubmitMemberDesignCommand : IRequest<Guid>
{
    public Guid GroupId { get; init; }

    public string CustomDesignJson { get; init; } = string.Empty;

    public decimal CalculatedPrice { get; init; }
}

public class SubmitMemberDesignCommandHandler : IRequestHandler<SubmitMemberDesignCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public SubmitMemberDesignCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Guid> Handle(SubmitMemberDesignCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Default(request.GroupId, nameof(request.GroupId));
        Guard.Against.Null(_user, nameof(_user));
        Guard.Against.NullOrWhiteSpace(_user.Id, nameof(_user.Id));

        // Load the group with its members and submissions (required for aggregate rule checks)
        var group = await _context.Groups
            .Include(g => g.Members)
            .Include(g => g.Submissions)
            .FirstOrDefaultAsync(g => g.PublicId == request.GroupId, cancellationToken);

        if (group is null)
        {
            throw new NotFoundException(nameof(Group), request.GroupId);
        }

        // Create the submission
        var submission = new OrderSubmission
        {
            GroupId = group.Id,
            UserId = _user.Id!,
            CustomDesignJson = request.CustomDesignJson,
            Price = request.CalculatedPrice,
            Status = SubmissionStatus.Submitted
        };

        // Delegate to the domain model to enforce business rules
        // This will throw domain exceptions if invariants are violated
        // It will also automatically transition the group status if all members have submitted
        group.AddSubmission(submission);

        await _context.SaveChangesAsync(cancellationToken);

        return submission.PublicId;
    }
}
