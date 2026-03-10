using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Application.Submissions.Commands.SubmitSingleOrder;

public record SubmitSingleOrderCommand : IRequest<Guid>
{
    public Guid ProductId { get; init; }

    public string CustomDesignJson { get; init; } = string.Empty;

    public decimal CalculatedPrice { get; init; }

    public string NameBehind { get; init; } = string.Empty;
}

public class SubmitSingleOrderCommandHandler : IRequestHandler<SubmitSingleOrderCommand, Guid>
{
    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public SubmitSingleOrderCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<Guid> Handle(SubmitSingleOrderCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Default(request.ProductId, nameof(request.ProductId));
        Guard.Against.Null(_user, nameof(_user));
        Guard.Against.NullOrWhiteSpace(_user.Id, nameof(_user.Id));

        // Verify the product exists and is active before creating the submission
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.PublicId == request.ProductId && p.IsActive, cancellationToken);

        if (product == null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(
                $"Product with ID {request.ProductId} not found or is not active.");
        }

        var submission = new OrderSubmission
        {
            GroupId = null,
            ProductId = product.Id,
            UserId = _user.Id!,
            CustomDesignJson = request.CustomDesignJson,
            NameBehind = request.NameBehind,
            Price = request.CalculatedPrice,
            Status = SubmissionStatus.ReadyForReview
        };

        submission.MarkSingleOrderReadyForReview();

        _context.OrderSubmissions.Add(submission);

        await _context.SaveChangesAsync(cancellationToken);

        return submission.PublicId;
    }
}

