using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Application.Common.Models;
using OjisanBackend.Application.Pricing.Queries.CalculatePrice;
using OjisanBackend.Domain.Entities;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Application.Payments.Commands.CreatePaymentSession;

public record CreatePaymentSessionCommand : IRequest<string>
{
    /// <summary>Group ID for group orders. Use this or OrderSubmissionId.</summary>
    public Guid? GroupId { get; init; }

    /// <summary>Order submission ID for single orders. Use this or GroupId.</summary>
    public Guid? OrderSubmissionId { get; init; }
}

public class CreatePaymentSessionCommandHandler : IRequestHandler<CreatePaymentSessionCommand, string>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly IOptions<PricingSettings> _pricingSettings;
    private readonly ISender _mediator;

    public CreatePaymentSessionCommandHandler(
        IApplicationDbContext context,
        IPaymentService paymentService,
        IOptions<PricingSettings> pricingSettings,
        ISender mediator)
    {
        _context = context;
        _paymentService = paymentService;
        _pricingSettings = pricingSettings;
        _mediator = mediator;
    }

    public async Task<string> Handle(CreatePaymentSessionCommand request, CancellationToken cancellationToken)
    {
        if (request.GroupId.HasValue)
        {
            return await HandleGroupPaymentAsync(request.GroupId.Value, cancellationToken);
        }

        if (request.OrderSubmissionId.HasValue)
        {
            return await HandleSingleOrderPaymentAsync(request.OrderSubmissionId.Value, cancellationToken);
        }

        throw new ArgumentException("Either GroupId or OrderSubmissionId must be provided.");
    }

    private async Task<string> HandleGroupPaymentAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await _context.Groups
            .Include(g => g.Submissions)
            .FirstOrDefaultAsync(g => g.PublicId == groupId, cancellationToken);

        if (group is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(Group), groupId);
        }

        if (group.Status != GroupStatus.Accepted)
        {
            throw new InvalidOperationException($"Group {groupId} is not in Accepted status. Current status: {group.Status}");
        }

        var totalGroupPrice = group.CalculateTotalGroupPrice();
        Guard.Against.NegativeOrZero(totalGroupPrice, nameof(totalGroupPrice), "Group must have at least one submission with a valid price.");

        var isPartialPayment = group.RequiresPartialPayment(_pricingSettings.Value.LargeGroupThreshold);
        var upfrontAmount = isPartialPayment ? totalGroupPrice / 2 : totalGroupPrice;
        Guard.Against.NegativeOrZero(upfrontAmount, nameof(upfrontAmount));

        var payment = new Payment
        {
            GroupId = group.Id,
            OrderSubmissionId = null,
            Amount = upfrontAmount,
            IsPartial = isPartialPayment,
            Phase = PaymentPhase.Upfront,
            Status = PaymentStatus.Pending
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        return await _paymentService.CreatePaymentSessionAsync(
            groupId,
            upfrontAmount,
            payment.PublicId.ToString(),
            cancellationToken);
    }

    private async Task<string> HandleSingleOrderPaymentAsync(Guid orderSubmissionId, CancellationToken cancellationToken)
    {
        var submission = await _context.OrderSubmissions
            .FirstOrDefaultAsync(s => s.PublicId == orderSubmissionId, cancellationToken);

        if (submission is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(OrderSubmission), orderSubmissionId);
        }

        if (submission.GroupId != null)
        {
            throw new InvalidOperationException("Use GroupId for group order payments.");
        }

        if (submission.Status != SubmissionStatus.Accepted)
        {
            throw new InvalidOperationException($"Order {orderSubmissionId} is not in Accepted status. Current status: {submission.Status}");
        }

        if (submission.IsPaid)
        {
            throw new InvalidOperationException("Order has already been paid.");
        }

        var amount = submission.Price;
        Guard.Against.NegativeOrZero(amount, nameof(amount), "Order must have a valid price.");

        var payment = new Payment
        {
            GroupId = null,
            OrderSubmissionId = submission.Id,
            Amount = amount,
            IsPartial = false,
            Phase = PaymentPhase.Upfront,
            Status = PaymentStatus.Pending
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        return await _paymentService.CreatePaymentSessionAsync(
            orderSubmissionId,
            amount,
            payment.PublicId.ToString(),
            cancellationToken);
    }
}
