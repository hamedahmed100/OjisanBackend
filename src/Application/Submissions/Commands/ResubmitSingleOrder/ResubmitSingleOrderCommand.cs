using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;
using OjisanBackend.Domain.Enums;
using OjisanBackend.Domain.Exceptions;

namespace OjisanBackend.Application.Submissions.Commands.ResubmitSingleOrder;

public record ResubmitBadgeInputDto
{
    public string ImageUrl { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
}

public record ResubmitSingleOrderCommand : IRequest
{
    public Guid SubmissionId { get; init; }
    public string CustomDesignJson { get; init; } = string.Empty;
    public List<ResubmitBadgeInputDto> Badges { get; init; } = new();
    public List<Guid> AddOnIds { get; init; } = new();
    public string? NameBehind { get; init; }
}

public class ResubmitSingleOrderCommandHandler : IRequestHandler<ResubmitSingleOrderCommand>
{
    private const int MinBadges = 3;
    private const int MaxBadges = 12;

    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public ResubmitSingleOrderCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task Handle(ResubmitSingleOrderCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Default(request.SubmissionId, nameof(request.SubmissionId));
        Guard.Against.NullOrWhiteSpace(request.CustomDesignJson, nameof(request.CustomDesignJson));
        Guard.Against.NullOrWhiteSpace(_user.Id, nameof(_user.Id));

        if (request.Badges.Count < MinBadges || request.Badges.Count > MaxBadges)
        {
            throw new BadgeCountValidationException(
                $"Badge count must be between {MinBadges} and {MaxBadges}. Received: {request.Badges.Count}.");
        }

        var submission = await _context.OrderSubmissions
            .Include(s => s.Badges)
            .Include(s => s.SelectedAddOns)
            .FirstOrDefaultAsync(s => s.PublicId == request.SubmissionId, cancellationToken);

        if (submission is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(OrderSubmission), request.SubmissionId);
        }

        if (submission.UserId != _user.Id)
        {
            throw new ForbiddenAccessException("You can only resubmit your own orders.");
        }

        if (submission.GroupId != null)
        {
            throw new InvalidOperationException("Use UpdateSubmission for group orders.");
        }

        if (submission.Status != SubmissionStatus.Rejected)
        {
            throw new InvalidOperationException($"Submission must be in Rejected status to resubmit. Current status: {submission.Status}.");
        }

        // Update design
        submission.CustomDesignJson = request.CustomDesignJson;
        submission.NameBehind = request.NameBehind ?? string.Empty;

        // Update badges
        _context.OrderBadges.RemoveRange(submission.Badges);
        foreach (var badge in request.Badges)
        {
            _context.OrderBadges.Add(new OrderBadge
            {
                OrderSubmissionId = submission.Id,
                ImageUrl = badge.ImageUrl,
                Comment = badge.Comment
            });
        }

        // Update add-ons
        var productId = submission.ProductId ?? 0;
        var addOns = await _context.ProductAddOns
            .Where(pa => request.AddOnIds.Contains(pa.PublicId)
                && (pa.ProductId == null || pa.ProductId == productId))
            .ToListAsync(cancellationToken);

        if (addOns.Count != request.AddOnIds.Count)
        {
            var foundIds = addOns.Select(pa => pa.PublicId).ToHashSet();
            var missing = request.AddOnIds.FirstOrDefault(id => !foundIds.Contains(id));
            throw new InvalidOperationException($"Add-on with ID {missing} not found or not valid for this product.");
        }

        _context.OrderSubmissionAddOns.RemoveRange(submission.SelectedAddOns);
        foreach (var addOn in addOns)
        {
            _context.OrderSubmissionAddOns.Add(new OrderSubmissionAddOn
            {
                OrderSubmissionId = submission.Id,
                ProductAddOnId = addOn.Id
            });
        }

        // Recalculate price
        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == productId && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("Product not found.");

        var basePrice = product.BasePrice;
        var badgeTotal = request.Badges.Count * product.BadgeUnitPrice;
        var addOnTotal = addOns.Sum(a => a.Price);
        submission.Price = basePrice + badgeTotal + addOnTotal;

        submission.ResubmitAfterRejection();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
