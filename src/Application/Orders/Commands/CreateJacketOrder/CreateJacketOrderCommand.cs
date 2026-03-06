using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;
using OjisanBackend.Domain.Enums;
using OjisanBackend.Domain.Exceptions;

namespace OjisanBackend.Application.Orders.Commands.CreateJacketOrder;

/// <summary>
/// Badge input for jacket order. Comment is mandatory.
/// </summary>
public record BadgeInputDto
{
    public string ImageUrl { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
}

/// <summary>
/// Full jacket configuration for a single order.
/// </summary>
public record CreateJacketOrderCommand : IRequest<CreateJacketOrderResult>
{
    public string CustomDesignJson { get; init; } = string.Empty;
    public List<BadgeInputDto> Badges { get; init; } = new();
    public List<Guid> AddOnIds { get; init; } = new();
    public string NameBehind { get; init; } = string.Empty;
}

public record CreateJacketOrderResult
{
    public Guid OrderId { get; init; }
    public decimal TotalPrice { get; init; }
}

public class CreateJacketOrderCommandHandler : IRequestHandler<CreateJacketOrderCommand, CreateJacketOrderResult>
{
    private const int MinBadges = 3;
    private const int MaxBadges = 11;

    private readonly IApplicationDbContext _context;
    private readonly IUser _user;

    public CreateJacketOrderCommandHandler(IApplicationDbContext context, IUser user)
    {
        _context = context;
        _user = user;
    }

    public async Task<CreateJacketOrderResult> Handle(CreateJacketOrderCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(_user.Id, nameof(_user.Id));

        // Validate badges: 3–11 required
        if (request.Badges.Count < MinBadges || request.Badges.Count > MaxBadges)
        {
            throw new BadgeCountValidationException(
                $"Badge count must be between {MinBadges} and {MaxBadges}. Received: {request.Badges.Count}.");
        }

        // Validate: each badge must have a non-empty comment
        var invalidBadge = request.Badges
            .Select((b, i) => (Index: i + 1, Badge: b))
            .FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Badge.Comment));
        if (invalidBadge.Badge != null!)
        {
            throw new BadgeCommentValidationException(
                $"Badge at index {invalidBadge.Index} requires a non-empty comment.");
        }

        // Get jacket product
        var jacket = await _context.Products
            .FirstOrDefaultAsync(p => p.Type == ProductType.Jacket && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException("No active jacket product found.");

        // Validate add-on IDs exist
        var addOns = await _context.ProductAddOns
            .Where(pa => request.AddOnIds.Contains(pa.PublicId))
            .ToListAsync(cancellationToken);
        if (addOns.Count != request.AddOnIds.Count)
        {
            var foundIds = addOns.Select(pa => pa.PublicId).ToHashSet();
            var missing = request.AddOnIds.FirstOrDefault(id => !foundIds.Contains(id));
            throw new InvalidOperationException($"Add-on with ID {missing} not found.");
        }

        // Calculate total: BasePrice + (BadgeCount * BadgeUnitPrice) + Sum(AddOns)
        var basePrice = jacket.BasePrice;
        var badgeTotal = request.Badges.Count * jacket.BadgeUnitPrice;
        var addOnTotal = addOns.Sum(a => a.Price);
        var totalPrice = basePrice + badgeTotal + addOnTotal;

        var submission = new OrderSubmission
        {
            GroupId = null,
            UserId = _user.Id!,
            CustomDesignJson = request.CustomDesignJson,
            NameBehind = request.NameBehind,
            Price = totalPrice,
            Status = SubmissionStatus.ReadyForReview
        };

        _context.OrderSubmissions.Add(submission);
        await _context.SaveChangesAsync(cancellationToken);

        foreach (var badge in request.Badges)
        {
            _context.OrderBadges.Add(new OrderBadge
            {
                OrderSubmissionId = submission.Id,
                ImageUrl = badge.ImageUrl,
                Comment = badge.Comment
            });
        }

        foreach (var addOn in addOns)
        {
            _context.OrderSubmissionAddOns.Add(new OrderSubmissionAddOn
            {
                OrderSubmissionId = submission.Id,
                ProductAddOnId = addOn.Id
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new CreateJacketOrderResult
        {
            OrderId = submission.PublicId,
            TotalPrice = totalPrice
        };
    }
}
