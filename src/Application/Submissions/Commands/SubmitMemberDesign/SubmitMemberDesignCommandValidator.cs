using FluentValidation;

namespace OjisanBackend.Application.Submissions.Commands.SubmitMemberDesign;

public class SubmitMemberDesignCommandValidator : AbstractValidator<SubmitMemberDesignCommand>
{
    public SubmitMemberDesignCommandValidator()
    {
        RuleFor(v => v.CustomDesignJson)
            .NotEmpty()
            .WithMessage("تصميم المنتج مطلوب.");

        RuleFor(v => v.CalculatedPrice)
            .GreaterThanOrEqualTo(0)
            .WithMessage("السعر يجب أن يكون أكبر من أو يساوي صفر.");
    }
}
