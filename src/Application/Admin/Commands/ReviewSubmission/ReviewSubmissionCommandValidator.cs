using FluentValidation;

namespace OjisanBackend.Application.Admin.Commands.ReviewSubmission;

public class ReviewSubmissionCommandValidator : AbstractValidator<ReviewSubmissionCommand>
{
    public ReviewSubmissionCommandValidator()
    {
        RuleFor(v => v.GroupId)
            .NotEmpty()
            .WithMessage("معرف المجموعة مطلوب.");

        RuleFor(v => v.SubmissionId)
            .NotEmpty()
            .WithMessage("معرف التقديم مطلوب.");

        RuleFor(v => v.Feedback)
            .NotEmpty()
            .When(v => !v.IsApproved)
            .WithMessage("التعليقات مطلوبة عند رفض التقديم.");
    }
}
