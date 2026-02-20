using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Application.Common.Models;
using OjisanBackend.Domain.Entities;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Application.Payments.Commands.RequestSecondPayment;

public record RequestSecondPaymentCommand : IRequest<string>
{
    public Guid GroupId { get; init; }
}

public class RequestSecondPaymentCommandHandler : IRequestHandler<RequestSecondPaymentCommand, string>
{
    private readonly IApplicationDbContext _context;
    private readonly IPaymentService _paymentService;
    private readonly IOptions<PricingSettings> _pricingSettings;

    public RequestSecondPaymentCommandHandler(
        IApplicationDbContext context,
        IPaymentService paymentService,
        IOptions<PricingSettings> pricingSettings)
    {
        _context = context;
        _paymentService = paymentService;
        _pricingSettings = pricingSettings;
    }

    public async Task<string> Handle(RequestSecondPaymentCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Default(request.GroupId, nameof(request.GroupId));

        var group = await _context.Groups
            .Include(g => g.Submissions)
            .FirstOrDefaultAsync(g => g.PublicId == request.GroupId, cancellationToken);

        if (group is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(Group), request.GroupId);
        }

        // Ensure this group actually qualifies for partial payment
        var requiresPartial = group.RequiresPartialPayment(_pricingSettings.Value.LargeGroupThreshold);
        if (!requiresPartial)
        {
            throw new InvalidOperationException($"Group {request.GroupId} does not qualify for a second payment.");
        }

        // Ensure there is a completed upfront payment
        var upfrontPayment = await _context.Payments
            .Where(p => p.GroupId == group.Id && p.Phase == PaymentPhase.Upfront)
            .OrderByDescending(p => p.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (upfrontPayment == null || upfrontPayment.Status != PaymentStatus.Completed)
        {
            throw new InvalidOperationException($"Group {request.GroupId} does not have a completed upfront payment.");
        }

        // Ensure no final payment exists yet
        var hasFinalPayment = await _context.Payments
            .AnyAsync(p => p.GroupId == group.Id && p.Phase == PaymentPhase.Final, cancellationToken);

        if (hasFinalPayment)
        {
            throw new InvalidOperationException($"Group {request.GroupId} already has a final payment record.");
        }

        var totalGroupPrice = group.CalculateTotalGroupPrice();
        Guard.Against.NegativeOrZero(totalGroupPrice, nameof(totalGroupPrice));

        var remainingAmount = totalGroupPrice - upfrontPayment.Amount;
        Guard.Against.NegativeOrZero(remainingAmount, nameof(remainingAmount));

        var finalPayment = new Payment
        {
            GroupId = group.Id,
            Amount = remainingAmount,
            IsPartial = true,
            Phase = PaymentPhase.Final,
            Status = PaymentStatus.Pending
        };

        _context.Payments.Add(finalPayment);
        await _context.SaveChangesAsync(cancellationToken);

        var checkoutUrl = await _paymentService.CreatePaymentSessionAsync(
            request.GroupId,
            remainingAmount,
            finalPayment.PublicId.ToString(),
            cancellationToken);

        finalPayment.MarkFinalPaymentRequested(checkoutUrl);

        await _context.SaveChangesAsync(cancellationToken);

        return checkoutUrl;
    }
}

