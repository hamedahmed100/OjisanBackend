using FluentValidation;

namespace OjisanBackend.Application.Submissions.Commands.SubmitSingleOrder;

public class SubmitSingleOrderCommandValidator : AbstractValidator<SubmitSingleOrderCommand>
{
    public SubmitSingleOrderCommandValidator()
    {
        RuleFor(v => v.CustomDesignJson)
            .NotEmpty();

        RuleFor(v => v.CalculatedPrice)
            .GreaterThanOrEqualTo(0);
    }
}

