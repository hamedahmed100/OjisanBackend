using FluentValidation;

namespace OjisanBackend.Application.Groups.Commands.CreateGroup;

public class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    public CreateGroupCommandValidator()
    {
        RuleFor(v => v.MaxMembers)
            .GreaterThanOrEqualTo(2)
            .WithMessage("عدد الأعضاء يجب أن يكون ٢ أو أكثر.");
    }
}

