using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Application.Common.Models;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Application.Pricing.Queries.CalculatePrice;

public record CalculatePriceQuery : IRequest<PriceCalculationResult>
{
    public Guid ProductId { get; init; }

    public List<Guid> SelectedOptionIds { get; init; } = new();

    public Guid? GroupId { get; init; }
}

public class CalculatePriceQueryHandler : IRequestHandler<CalculatePriceQuery, PriceCalculationResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IOptions<PricingSettings> _pricingSettings;

    public CalculatePriceQueryHandler(
        IApplicationDbContext context,
        IOptions<PricingSettings> pricingSettings)
    {
        _context = context;
        _pricingSettings = pricingSettings;
    }

    public async Task<PriceCalculationResult> Handle(CalculatePriceQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Default(request.ProductId, nameof(request.ProductId));

        // Fetch the product with its options
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Options)
            .FirstOrDefaultAsync(p => p.PublicId == request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new NotFoundException(nameof(Product), request.ProductId);
        }

        // Calculate base price
        var totalPrice = product.BasePrice;

        // Add costs for selected options
        if (request.SelectedOptionIds.Any())
        {
            var selectedOptions = await _context.ProductOptions
                .AsNoTracking()
                .Where(po => request.SelectedOptionIds.Contains(po.PublicId) && po.ProductId == product.Id)
                .ToListAsync(cancellationToken);

            // Validate all selected options exist
            if (selectedOptions.Count != request.SelectedOptionIds.Count)
            {
                var foundIds = selectedOptions.Select(o => o.PublicId).ToList();
                var missingIds = request.SelectedOptionIds.Except(foundIds).ToList();
                throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(
                    $"One or more product options not found: {string.Join(", ", missingIds)}");
            }

            totalPrice += selectedOptions.Sum(o => o.AdditionalCost);
        }

        // Determine payment split based on group size
        decimal upfrontAmount;
        decimal remainingAmount;
        bool isPartialPayment;

        if (request.GroupId.HasValue)
        {
            // Fetch the group to check if it qualifies for partial payment
            var group = await _context.Groups
                .AsNoTracking()
                .FirstOrDefaultAsync(g => g.PublicId == request.GroupId.Value, cancellationToken);

            if (group != null && group.RequiresPartialPayment(_pricingSettings.Value.LargeGroupThreshold))
            {
                // Large group: 50/50 split
                upfrontAmount = totalPrice / 2;
                remainingAmount = totalPrice / 2;
                isPartialPayment = true;
            }
            else
            {
                // Small group or group not found: full payment upfront
                upfrontAmount = totalPrice;
                remainingAmount = 0;
                isPartialPayment = false;
            }
        }
        else
        {
            // Single order: full payment upfront
            upfrontAmount = totalPrice;
            remainingAmount = 0;
            isPartialPayment = false;
        }

        return new PriceCalculationResult
        {
            TotalPrice = totalPrice,
            UpfrontAmount = upfrontAmount,
            RemainingAmount = remainingAmount,
            IsPartialPayment = isPartialPayment
        };
    }
}
