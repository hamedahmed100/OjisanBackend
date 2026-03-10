using System.Text.Json;
using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Application.Groups.Common;
using OjisanBackend.Domain.Entities;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Application.Groups.Commands.CreateGroup;

public record BaseDesignDto
{
    public string? Color { get; init; }

    public string? Material { get; init; }

    public string? Pattern { get; init; }
}

/// <summary>
/// Request to create a group. MemberCount and IsUniformColorSelected drive discount eligibility (5+ uniform colour rule).
/// </summary>
public record CreateGroupCommand : IRequest<CreateGroupResult>
{
    /// <summary>
    /// Number of members (2–30). Also used as MaxMembers. Use this or MaxMembers (backward compat).
    /// </summary>
    public int MemberCount { get; init; }

    /// <summary>
    /// Backward compatibility: same as MemberCount. If MemberCount is 0, this value is used.
    /// </summary>
    public int MaxMembers { get; init; }

    /// <summary>
    /// Display name for the group (e.g. "Team Alpha").
    /// </summary>
    public string Name { get; init; } = string.Empty;

    /// <summary>
    /// Whether uniform colour is selected (Jacket, Sleeves, مطاط الاكمام). Required for discount.
    /// </summary>
    public bool IsUniformColorSelected { get; init; }

    /// <summary>
    /// Product public ID (from GetActiveProducts or discount-eligibility). Use this, not internal ProductId.
    /// </summary>
    public Guid ProductPublicId { get; init; }

    public BaseDesignDto? BaseDesign { get; init; }

    /// <summary>
    /// Optional common name behind for all members (used when IsUniformColorSelected is true).
    /// </summary>
    public string? NameBehind { get; init; }

    /// <summary>
    /// Optional add-on public IDs for pricing breakdown. Add-on total is per member.
    /// </summary>
    public List<Guid> AddOnIds { get; init; } = new();

    /// <summary>
    /// Effective member count: MemberCount if set (>= 2), otherwise MaxMembers.
    /// </summary>
    internal int EffectiveMemberCount => MemberCount >= 2 ? MemberCount : MaxMembers;
}

public class CreateGroupCommandHandler : IRequestHandler<CreateGroupCommand, CreateGroupResult>
{
    private const int MinMemberCount = 2;
    private const int MaxMemberCount = 30;

    private readonly IApplicationDbContext _context;
    private readonly IUser _user;
    private readonly IInviteCodeService _inviteCodeService;
    private readonly IGroupPricingService _groupPricingService;

    public CreateGroupCommandHandler(
        IApplicationDbContext context,
        IUser user,
        IInviteCodeService inviteCodeService,
        IGroupPricingService groupPricingService)
    {
        _context = context;
        _user = user;
        _inviteCodeService = inviteCodeService;
        _groupPricingService = groupPricingService;
    }

    public async Task<CreateGroupResult> Handle(CreateGroupCommand request, CancellationToken cancellationToken)
    {
        var memberCount = request.EffectiveMemberCount;
        if (memberCount < MinMemberCount || memberCount > MaxMemberCount)
        {
            throw new BadRequestException(
                $"Group size must be between {MinMemberCount} and {MaxMemberCount}. Received: {memberCount}.");
        }

        Guard.Against.Null(_user, nameof(_user));
        Guard.Against.NullOrWhiteSpace(_user.Id, nameof(_user.Id));

        var product = await _context.Products
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.PublicId == request.ProductPublicId && p.IsActive, cancellationToken);

        if (product == null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(Product), request.ProductPublicId);
        }

        var asOfUtc = DateTime.UtcNow;
        var pricingResult = await _groupPricingService.CalculateGroupPricingAsync(
            product.Id,
            memberCount,
            request.IsUniformColorSelected,
            request.AddOnIds,
            asOfUtc,
            cancellationToken);

        await using var transaction = await _context.Database.BeginTransactionAsync(cancellationToken);
        try
        {
            var group = new Group
            {
                Name = request.Name?.Trim() ?? string.Empty,
                LeaderUserId = _user.Id!,
                ProductId = product.Id,
                MaxMembers = memberCount,
                MemberCount = memberCount,
                IsUniformColorSelected = request.IsUniformColorSelected,
                AppliedDiscountPercentage = pricingResult.Breakdown.AppliedDiscountPercentage,
                DiscountExpiryDate = pricingResult.AppliedPromotion?.EndDate,
                BaseDesignJson = JsonSerializer.Serialize(
                    request.BaseDesign ?? new BaseDesignDto(),
                    new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }),
                NameBehind = request.IsUniformColorSelected ? request.NameBehind?.Trim() : null,
                Status = GroupStatus.Recruiting
            };

            _context.Groups.Add(group);
            await _context.SaveChangesAsync(cancellationToken);

            group.InviteCode = _inviteCodeService.GenerateInviteCode(group.Id);

            var leaderSubmission = new OrderSubmission
            {
                GroupId = group.Id,
                UserId = _user.Id!,
                CustomDesignJson = string.Empty,
                Price = 0,
                Status = SubmissionStatus.Draft
            };
            _context.OrderSubmissions.Add(leaderSubmission);

            await _context.SaveChangesAsync(cancellationToken);

            await transaction.CommitAsync(cancellationToken);

            return new CreateGroupResult
            {
                GroupId = group.PublicId,
                PriceBreakdown = pricingResult.Breakdown
            };
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }
}

