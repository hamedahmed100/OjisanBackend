using FluentValidation;

namespace OjisanBackend.Application.Admin.Commands.ReviewGroupBatch;

public class ReviewGroupBatchCommandValidator : AbstractValidator<ReviewGroupBatchCommand>
{
    public ReviewGroupBatchCommandValidator()
    {
        RuleFor(v => v.GroupId)
            .NotEmpty()
            .WithMessage("معرف المجموعة مطلوب.");

        RuleFor(v => v.Decisions)
            .NotEmpty()
            .WithMessage("يجب تقديم قرار واحد على الأقل لكل عضو.");
    }
}
