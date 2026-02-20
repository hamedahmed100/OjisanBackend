using FluentValidation;

namespace OjisanBackend.Application.Pricing.Queries.CalculatePrice;

public class CalculatePriceQueryValidator : AbstractValidator<CalculatePriceQuery>
{
    public CalculatePriceQueryValidator()
    {
        RuleFor(v => v.ProductId)
            .NotEmpty()
            .WithMessage("معرف المنتج مطلوب.");
    }
}
