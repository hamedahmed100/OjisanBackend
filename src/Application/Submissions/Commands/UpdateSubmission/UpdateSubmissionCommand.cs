using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Application.Submissions.Commands.SubmitMemberDesign;
using OjisanBackend.Domain.Entities;
using OjisanBackend.Domain.Exceptions;

namespace OjisanBackend.Application.Submissions.Commands.UpdateSubmission;

public record UpdateSubmissionCommand : IRequest
{
    public Guid GroupId { get; init; }

    public Guid SubmissionId { get; init; }

    public string NewCustomDesignJson { get; init; } = string.Empty;

    /// <summary>
    /// Optional: replace badges when resubmitting. If provided, must be 3–11 with non-empty comments.
    /// </summary>
    public List<MemberBadgeInputDto>? Badges { get; init; }

    /// <summary>
    /// Optional: replace add-ons when resubmitting.
    /// </summary>
    public List<Guid>? AddOnIds { get; init; }

    /// <summary>
    /// Optional: update name behind.
    /// </summary>
    public string? NameBehind { get; init; }
}

public class UpdateSubmissionCommandHandler : IRequestHandler<UpdateSubmissionCommand>
{
    private const int MinBadges = 3;
    private const int MaxBadges = 11;

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

        var group = await _context.Groups
            .Include(g => g.Submissions)
            .FirstOrDefaultAsync(g => g.PublicId == request.GroupId, cancellationToken);

        if (group is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(Group), request.GroupId);
        }

        var submission = await _context.OrderSubmissions
            .Include(s => s.Badges)
            .Include(s => s.SelectedAddOns)
                .ThenInclude(sa => sa.ProductAddOn)
            .FirstOrDefaultAsync(s => s.PublicId == request.SubmissionId && s.GroupId == group.Id, cancellationToken);

        if (submission is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(OrderSubmission), request.SubmissionId);
        }

        if (submission.UserId != _user.Id)
        {
            throw new ForbiddenAccessException("You can only update your own submissions.");
        }

        var customDesignJson = group.IsUniformColorSelected ? group.BaseDesignJson : request.NewCustomDesignJson;
        submission.UpdateDesign(customDesignJson);

        if (group.IsUniformColorSelected)
        {
            submission.NameBehind = group.NameBehind ?? string.Empty;
        }
        else if (request.NameBehind != null)
        {
            submission.NameBehind = request.NameBehind;
        }

        if (request.Badges != null)
        {
            if (request.Badges.Count < MinBadges || request.Badges.Count > MaxBadges)
            {
                throw new BadgeCountValidationException(
                    $"Badge count must be between {MinBadges} and {MaxBadges}. Received: {request.Badges.Count}.");
            }

            var invalidBadge = request.Badges
                .Select((b, i) => (Index: i + 1, Badge: b))
                .FirstOrDefault(x => string.IsNullOrWhiteSpace(x.Badge.Comment));
            if (invalidBadge.Badge != null!)
            {
                throw new BadgeCommentValidationException(
                    $"Badge at index {invalidBadge.Index} requires a non-empty comment.");
            }

            _context.OrderBadges.RemoveRange(submission.Badges);
            foreach (var badge in request.Badges)
            {
                submission.Badges.Add(new OrderBadge
                {
                    ImageUrl = badge.ImageUrl,
                    Comment = badge.Comment
                });
            }
        }

        List<ProductAddOn>? addOns = null;
        if (request.AddOnIds != null)
        {
            addOns = await _context.ProductAddOns
                .Where(pa => request.AddOnIds.Contains(pa.PublicId)
                    && (pa.ProductId == null || pa.ProductId == group.ProductId))
                .ToListAsync(cancellationToken);
            if (addOns.Count != request.AddOnIds.Count)
            {
                var found = addOns.Select(pa => pa.PublicId).ToHashSet();
                var missing = request.AddOnIds.First(id => !found.Contains(id));
                throw new InvalidOperationException($"Add-on with ID {missing} not found or not valid for this product.");
            }

            _context.OrderSubmissionAddOns.RemoveRange(submission.SelectedAddOns);
            foreach (var addOn in addOns)
            {
                submission.SelectedAddOns.Add(new OrderSubmissionAddOn
                {
                    ProductAddOnId = addOn.Id
                });
            }
        }

        if (request.Badges != null || request.AddOnIds != null)
        {
            var product = await _context.Products
                .AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == group.ProductId && p.IsActive, cancellationToken)
                ?? throw new InvalidOperationException("Product not found.");

            var discountMultiplier = 1m - (group.AppliedDiscountPercentage / 100m);
            var basePricePerMember = product.BasePrice * discountMultiplier;
            var badgeCount = submission.Badges.Count;
            var addOnTotal = addOns != null
                ? addOns.Sum(a => a.Price)
                : submission.SelectedAddOns.Where(sa => sa.ProductAddOn != null).Sum(sa => sa.ProductAddOn!.Price);
            submission.Price = basePricePerMember + (badgeCount * product.BadgeUnitPrice) + addOnTotal;
        }

        group.EvaluateGroupStatus();

        await _context.SaveChangesAsync(cancellationToken);
    }
}
