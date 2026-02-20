using FluentValidation;

namespace OjisanBackend.Application.Groups.Commands.JoinGroup;

public class JoinGroupCommandValidator : AbstractValidator<JoinGroupCommand>
{
    public JoinGroupCommandValidator()
    {
        RuleFor(v => v.InviteCode)
            .NotEmpty()
            .WithMessage("رمز الدعوة مطلوب.");
    }
}
