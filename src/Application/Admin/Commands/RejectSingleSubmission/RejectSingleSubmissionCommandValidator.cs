using FluentValidation;

namespace OjisanBackend.Application.Admin.Commands.RejectSingleSubmission;

public class RejectSingleSubmissionCommandValidator : AbstractValidator<RejectSingleSubmissionCommand>
{
    public RejectSingleSubmissionCommandValidator()
    {
        RuleFor(v => v.SubmissionId)
            .NotEmpty()
            .WithMessage("معرف التقديم مطلوب.");

        RuleFor(v => v.Feedback)
            .NotEmpty()
            .WithMessage("التعليقات مطلوبة عند رفض التقديم.");
    }
}
