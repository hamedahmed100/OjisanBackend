using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;
using OjisanBackend.Domain.Enums;
using OjisanBackend.Domain.Exceptions;

namespace OjisanBackend.Application.Submissions.Commands.SubmitMemberDesign;

/// <summary>
/// Badge input for group member submission. Same as CreateJacketOrder.
/// </summary>
public record MemberBadgeInputDto
{
    public string ImageUrl { get; init; } = string.Empty;
    public string Comment { get; init; } = string.Empty;
}

/// <summary>
/// Submit a member's design for a group. Same fields as single jacket order: badges, add-ons, design.
/// When IsUniformColorSelected, CustomDesignJson is ignored and BaseDesignJson is used (members cannot change colors).
/// </summary>
public record SubmitMemberDesignCommand : IRequest<Guid>
{
    public Guid GroupId { get; init; }

    /// <summary>
    /// Custom design JSON (colors, materials, patterns). Ignored when group has uniform color - base design is used.
    /// </summary>
    public string CustomDesignJson { get; init; } = string.Empty;

    /// <summary>
    /// Badges: 3–11 required, each with non-empty comment.
    /// </summary>
    public List<MemberBadgeInputDto> Badges { get; init; } = new();

    /// <summary>
    /// Optional add-on public IDs. Must be valid for the group's product.
    /// </summary>
    public List<Guid> AddOnIds { get; init; } = new();

    /// <summary>
    /// Name to print on the back of the jacket.
    /// </summary>
    public string NameBehind { get; init; } = string.Empty;
}

public class SubmitMemberDesignCommandHandler : IRequestHandler<SubmitMemberDesignCommand, Guid>
{
    private const int MinBadges = 3;
    private const int MaxBadges = 11;

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

        var group = await _context.Groups
            .Include(g => g.Members)
            .Include(g => g.Submissions)
            .FirstOrDefaultAsync(g => g.PublicId == request.GroupId, cancellationToken);

        if (group is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(Group), request.GroupId);
        }

        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == group.ProductId && p.IsActive, cancellationToken)
            ?? throw new InvalidOperationException($"Product for group not found or inactive.");

        var addOns = new List<ProductAddOn>();
        if (request.AddOnIds.Count > 0)
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
        }

        var discountMultiplier = 1m - (group.AppliedDiscountPercentage / 100m);
        var basePricePerMember = product.BasePrice * discountMultiplier;
        var badgeTotal = request.Badges.Count * product.BadgeUnitPrice;
        var addOnTotal = addOns.Sum(a => a.Price);
        var totalPrice = basePricePerMember + badgeTotal + addOnTotal;

        if (!group.IsUniformColorSelected && string.IsNullOrWhiteSpace(request.CustomDesignJson))
        {
            throw new ArgumentException("Custom design is required when group does not have uniform color.");
        }

        var customDesignJson = group.IsUniformColorSelected ? group.BaseDesignJson : request.CustomDesignJson;
        var nameBehind = group.IsUniformColorSelected ? (group.NameBehind ?? string.Empty) : request.NameBehind;

        var existingDraft = await _context.OrderSubmissions
            .Include(s => s.Badges)
            .Include(s => s.SelectedAddOns)
            .FirstOrDefaultAsync(s => s.GroupId == group.Id
                && s.UserId == _user.Id
                && s.Status == SubmissionStatus.Draft, cancellationToken);

        OrderSubmission submission;

        if (existingDraft != null)
        {
            submission = existingDraft;
            submission.CustomDesignJson = customDesignJson;
            submission.NameBehind = nameBehind;
            submission.Price = totalPrice;
            submission.Status = SubmissionStatus.Submitted;

            _context.OrderBadges.RemoveRange(submission.Badges);
            _context.OrderSubmissionAddOns.RemoveRange(submission.SelectedAddOns);
            submission.Badges.Clear();
            submission.SelectedAddOns.Clear();
        }
        else
        {
            submission = new OrderSubmission
            {
                GroupId = group.Id,
                UserId = _user.Id!,
                CustomDesignJson = customDesignJson,
                NameBehind = nameBehind,
                Price = totalPrice,
                Status = SubmissionStatus.Submitted
            };
        }

        foreach (var badge in request.Badges)
        {
            submission.Badges.Add(new OrderBadge
            {
                ImageUrl = badge.ImageUrl,
                Comment = badge.Comment
            });
        }

        foreach (var addOn in addOns)
        {
            submission.SelectedAddOns.Add(new OrderSubmissionAddOn
            {
                ProductAddOnId = addOn.Id
            });
        }

        if (existingDraft != null)
        {
            group.EvaluateReadyForReview();
        }
        else
        {
            group.AddSubmission(submission);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return submission.PublicId;
    }
}
