using FluentValidation;

namespace OjisanBackend.Application.Products.Commands.CreateProduct;

public class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator()
    {
        RuleFor(v => v.Name)
            .NotEmpty()
            .WithMessage("اسم المنتج مطلوب.")
            .MaximumLength(200)
            .WithMessage("اسم المنتج يجب ألا يتجاوز 200 حرف.");

        RuleFor(v => v.BasePrice)
            .GreaterThan(0)
            .WithMessage("السعر الأساسي يجب أن يكون أكبر من صفر.");
    }
}
