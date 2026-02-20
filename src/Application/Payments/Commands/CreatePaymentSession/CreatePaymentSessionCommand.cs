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
    public Guid GroupId { get; init; }
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
        Guard.Against.Default(request.GroupId, nameof(request.GroupId));

        // Fetch the group with its submissions to calculate the true total price
        var group = await _context.Groups
            .Include(g => g.Submissions)
            .FirstOrDefaultAsync(g => g.PublicId == request.GroupId, cancellationToken);

        if (group is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(Group), request.GroupId);
        }

        // Ensure group is in Accepted status (ready for payment)
        if (group.Status != GroupStatus.Accepted)
        {
            throw new InvalidOperationException($"Group {request.GroupId} is not in Accepted status. Current status: {group.Status}");
        }

        // Calculate the total group price by summing all submission prices
        // Each OrderSubmission already has its calculated Price saved from when the member submitted it
        var totalGroupPrice = group.CalculateTotalGroupPrice();

        Guard.Against.NegativeOrZero(totalGroupPrice, nameof(totalGroupPrice), "Group must have at least one submission with a valid price.");

        // Determine if this is a partial payment (50/50 split) based on group size
        var isPartialPayment = group.RequiresPartialPayment(_pricingSettings.Value.LargeGroupThreshold);
        decimal upfrontAmount;

        if (isPartialPayment)
        {
            // Large group: 50/50 split - charge 50% upfront
            upfrontAmount = totalGroupPrice / 2;
        }
        else
        {
            // Small group or single order: full payment upfront
            upfrontAmount = totalGroupPrice;
        }

        Guard.Against.NegativeOrZero(upfrontAmount, nameof(upfrontAmount));

        // Create payment record
        var payment = new Payment
        {
            GroupId = group.Id,
            Amount = upfrontAmount,
            IsPartial = isPartialPayment,
            Phase = PaymentPhase.Upfront,
            Status = PaymentStatus.Pending
        };

        _context.Payments.Add(payment);
        await _context.SaveChangesAsync(cancellationToken);

        // Create payment session with Fatorah
        var checkoutUrl = await _paymentService.CreatePaymentSessionAsync(
            request.GroupId,
            upfrontAmount,
            payment.PublicId.ToString(), // Use payment PublicId as merchant_resource_id
            cancellationToken);

        // Update payment with transaction ID if returned by Fatorah
        // For now, we'll store the checkout URL reference or transaction ID when webhook is received

        return checkoutUrl;
    }
}
