using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Application.Admin.Queries.GetAllSubmissionsUnderReview;

public static class AdminKanbanColumns
{
    public const string Submitted = "submitted";
    public const string ReadyForReview = "readyForReview";
    public const string Accepted = "accepted";
    public const string Rejected = "rejected";
    public const string PendingPayment = "pendingPayment";
    public const string InProcess = "inProcess";
    public const string Shipping = "shipping";
    public const string Shipped = "shipped";

    public static readonly IReadOnlyList<string> Ordered = new[]
    {
        Submitted,
        ReadyForReview,
        Accepted,
        Rejected,
        PendingPayment,
        InProcess,
        Shipping,
        Shipped
    };
}

public record AdminKanbanCardDto
{
    /// <summary>"single" or "group".</summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>For Kind="single": submission public ID. For Kind="group": group public ID.</summary>
    public Guid Id { get; init; }

    /// <summary>Display title for the card (e.g. group name).</summary>
    public string Title { get; init; } = string.Empty;

    /// <summary>Primary user for the card (single userId or group leader userId).</summary>
    public string UserId { get; init; } = string.Empty;

    public decimal Price { get; init; }

    public DateTimeOffset CreatedAt { get; init; }

    /// <summary>Derived Kanban column id.</summary>
    public string ColumnId { get; init; } = string.Empty;

    public string? BadgeImageUrl { get; init; }

    // Optional group-specific fields
    public string? InviteCode { get; init; }
    public int? MaxMembers { get; init; }
    public int? CurrentSubmissions { get; init; }

    // Optional fulfillment fields (single + group)
    public bool? IsPaid { get; init; }
    public string? TrackingNumber { get; init; }
    public string? ShippingLabelUrl { get; init; }
    public DateTime? ShippedAt { get; init; }
}

public record AdminKanbanColumnDto
{
    public string Id { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public List<AdminKanbanCardDto> Items { get; init; } = new();
}

public record AdminKanbanBoardResponse
{
    public List<AdminKanbanColumnDto> Columns { get; init; } = new();
}

public record GetAllSubmissionsUnderReviewQuery : IRequest<AdminKanbanBoardResponse>;

public class GetAllSubmissionsUnderReviewQueryHandler : IRequestHandler<GetAllSubmissionsUnderReviewQuery, AdminKanbanBoardResponse>
{
    private readonly IApplicationDbContext _context;

    public GetAllSubmissionsUnderReviewQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<AdminKanbanBoardResponse> Handle(GetAllSubmissionsUnderReviewQuery request, CancellationToken cancellationToken)
    {
        // Single orders (GroupId == null): include all states needed for admin pipeline
        var singleData = await _context.OrderSubmissions
            .AsNoTracking()
            .Where(s => s.GroupId == null &&
                        (s.Status == SubmissionStatus.Submitted ||
                         s.Status == SubmissionStatus.ReadyForReview ||
                         s.Status == SubmissionStatus.Accepted ||
                         s.Status == SubmissionStatus.Rejected))
            .ToListAsync(cancellationToken);

        // Groups: include recruiting (with submissions), ready-for-review, accepted, rejected, finalized (paid)
        var groupsData = await _context.Groups
            .AsNoTracking()
            .Include(g => g.Submissions)
            .Where(g =>
                (g.Status == GroupStatus.Recruiting && g.Submissions.Any(s => s.Status != SubmissionStatus.Draft)) ||
                g.Status == GroupStatus.ReadyForReview ||
                g.Status == GroupStatus.Accepted ||
                g.Status == GroupStatus.Rejected ||
                g.Status == GroupStatus.Finalized)
            .ToListAsync(cancellationToken);

        // Payments: used to derive "accepted" vs "pending payment" split.
        var groupInternalIds = groupsData.Select(g => g.Id).ToList();
        var groupPayments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.GroupId.HasValue && groupInternalIds.Contains(p.GroupId.Value))
            .ToListAsync(cancellationToken);

        var singleInternalIds = singleData.Select(s => s.Id).ToList();
        var singlePayments = await _context.Payments
            .AsNoTracking()
            .Where(p => p.OrderSubmissionId.HasValue && singleInternalIds.Contains(p.OrderSubmissionId.Value))
            .ToListAsync(cancellationToken);

        var cards = new List<AdminKanbanCardDto>();

        foreach (var s in singleData)
        {
            var paymentsForSubmission = singlePayments.Where(p => p.OrderSubmissionId == s.Id).ToList();
            var columnId = DeriveSingleOrderColumn(s.Status, s.IsPaid, paymentsForSubmission, s.TrackingNumber, s.ShippedAt);
            cards.Add(new AdminKanbanCardDto
            {
                Kind = "single",
                Id = s.PublicId,
                Title = "Single Order",
                UserId = s.UserId,
                Price = s.Price,
                CreatedAt = s.Created,
                ColumnId = columnId,
                BadgeImageUrl = ExtractBadgeImageUrl(s.CustomDesignJson),
                IsPaid = s.IsPaid,
                TrackingNumber = s.TrackingNumber,
                ShippingLabelUrl = s.ShippingLabelUrl,
                ShippedAt = s.ShippedAt
            });
        }

        foreach (var g in groupsData)
        {
            var totalPrice = g.Submissions.Sum(x => x.Price);
            var currentSubmissions = g.Submissions.Count(s => s.Status != SubmissionStatus.Draft);
            var badgeImageUrl = g.Submissions
                .Select(s => ExtractBadgeImageUrl(s.CustomDesignJson))
                .FirstOrDefault(url => !string.IsNullOrWhiteSpace(url));

            var paymentsForGroup = groupPayments.Where(p => p.GroupId == g.Id).ToList();
            var columnId = DeriveGroupColumn(g.Status, paymentsForGroup, g.TrackingNumber, g.ShippedAt);

            cards.Add(new AdminKanbanCardDto
            {
                Kind = "group",
                Id = g.PublicId,
                Title = g.Name,
                UserId = g.LeaderUserId,
                Price = totalPrice,
                CreatedAt = g.Created,
                ColumnId = columnId,
                BadgeImageUrl = badgeImageUrl,
                InviteCode = g.InviteCode,
                MaxMembers = g.MaxMembers,
                CurrentSubmissions = currentSubmissions,
                IsPaid = g.Status == GroupStatus.Finalized,
                TrackingNumber = g.TrackingNumber,
                ShippingLabelUrl = g.ShippingLabelUrl,
                ShippedAt = g.ShippedAt
            });
        }

        var columns = AdminKanbanColumns.Ordered
            .Select(id => new AdminKanbanColumnDto
            {
                Id = id,
                Title = id,
                Items = cards
                    .Where(c => string.Equals(c.ColumnId, id, StringComparison.OrdinalIgnoreCase))
                    .OrderByDescending(c => c.CreatedAt)
                    .ToList()
            })
            .ToList();

        return new AdminKanbanBoardResponse
        {
            Columns = columns
        };
    }

    private static string? ExtractBadgeImageUrl(string customDesignJson)
    {
        if (string.IsNullOrWhiteSpace(customDesignJson))
        {
            return null;
        }

        var urlMatch = System.Text.RegularExpressions.Regex.Match(
            customDesignJson,
            @"""badgeImageUrl""\s*:\s*""([^""]+)""",
            System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        return urlMatch.Success ? urlMatch.Groups[1].Value : null;
    }

    private static string DeriveSingleOrderColumn(
        SubmissionStatus status,
        bool isPaid,
        List<OjisanBackend.Domain.Entities.Payment> payments,
        string? trackingNumber,
        DateTime? shippedAt)
    {
        if (shippedAt.HasValue)
        {
            return AdminKanbanColumns.Shipped;
        }

        if (!string.IsNullOrWhiteSpace(trackingNumber))
        {
            return AdminKanbanColumns.Shipping;
        }

        if (isPaid)
        {
            return AdminKanbanColumns.InProcess;
        }

        var hasPendingPayment = payments.Any(p => p.Status == PaymentStatus.Pending);
        if (hasPendingPayment)
        {
            return AdminKanbanColumns.PendingPayment;
        }

        return status switch
        {
            SubmissionStatus.Submitted => AdminKanbanColumns.Submitted,
            SubmissionStatus.ReadyForReview => AdminKanbanColumns.ReadyForReview,
            SubmissionStatus.Rejected => AdminKanbanColumns.Rejected,
            SubmissionStatus.Accepted => AdminKanbanColumns.Accepted,
            _ => AdminKanbanColumns.Submitted
        };
    }

    private static string DeriveGroupColumn(
        GroupStatus status,
        List<OjisanBackend.Domain.Entities.Payment> payments,
        string? trackingNumber,
        DateTime? shippedAt)
    {
        if (shippedAt.HasValue)
        {
            return AdminKanbanColumns.Shipped;
        }

        if (!string.IsNullOrWhiteSpace(trackingNumber))
        {
            return AdminKanbanColumns.Shipping;
        }

        if (status == GroupStatus.Finalized)
        {
            return AdminKanbanColumns.InProcess;
        }

        if (status == GroupStatus.Accepted)
        {
            var hasPendingPayment = payments.Any(p => p.Status == PaymentStatus.Pending);
            return hasPendingPayment ? AdminKanbanColumns.PendingPayment : AdminKanbanColumns.Accepted;
        }

        return status switch
        {
            GroupStatus.Recruiting => AdminKanbanColumns.Submitted,
            GroupStatus.ReadyForReview => AdminKanbanColumns.ReadyForReview,
            GroupStatus.Rejected => AdminKanbanColumns.Rejected,
            _ => AdminKanbanColumns.Submitted
        };
    }
}
