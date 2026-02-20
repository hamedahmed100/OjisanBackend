using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Application.Admin.Commands.ToggleEditLock;

public record ToggleEditLockCommand : IRequest
{
    public Guid SubmissionId { get; init; }

    /// <summary>
    /// When true, editing is enabled (unlocked). When false, editing is disabled (locked).
    /// </summary>
    public bool EnableEdit { get; init; }
}

public class ToggleEditLockCommandHandler : IRequestHandler<ToggleEditLockCommand>
{
    private readonly IApplicationDbContext _context;

    public ToggleEditLockCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(ToggleEditLockCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Default(request.SubmissionId, nameof(request.SubmissionId));

        var submission = await _context.OrderSubmissions
            .FirstOrDefaultAsync(s => s.PublicId == request.SubmissionId, cancellationToken);

        if (submission is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(OrderSubmission), request.SubmissionId);
        }

        if (request.EnableEdit)
        {
            submission.UnlockEdit();
        }
        else
        {
            submission.LockEdit();
        }

        await _context.SaveChangesAsync(cancellationToken);
    }
}

