using FluentValidation;

namespace OjisanBackend.Application.Submissions.Commands.ResubmitSingleOrder;

public class ResubmitSingleOrderCommandValidator : AbstractValidator<ResubmitSingleOrderCommand>
{
    public ResubmitSingleOrderCommandValidator()
    {
        RuleFor(v => v.SubmissionId)
            .NotEmpty()
            .WithMessage("معرف التقديم مطلوب.");

        RuleFor(v => v.CustomDesignJson)
            .NotEmpty()
            .WithMessage("تصميم السترة مطلوب.");

        RuleFor(v => v.Badges)
            .Must(b => b.Count >= 3 && b.Count <= 12)
            .WithMessage("يجب أن يكون عدد الشارات بين 3 و 12.");
    }
}
