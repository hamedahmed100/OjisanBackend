using FluentValidation;

namespace OjisanBackend.Application.Groups.Commands.CreateGroup;

public class CreateGroupCommandValidator : AbstractValidator<CreateGroupCommand>
{
    private const int MinMemberCount = 2;
    private const int MaxMemberCount = 30;

    public CreateGroupCommandValidator()
    {
        RuleFor(v => v.ProductPublicId)
            .NotEmpty()
            .WithMessage("Product is required.");

        RuleFor(v => v.Name)
            .NotEmpty()
            .WithMessage("Group name is required.")
            .MaximumLength(200)
            .WithMessage("Group name must not exceed 200 characters.");

        RuleFor(v => v.EffectiveMemberCount)
            .InclusiveBetween(MinMemberCount, MaxMemberCount)
            .WithMessage($"Group size must be between {MinMemberCount} and {MaxMemberCount}.");

        RuleFor(v => v.NameBehind)
            .NotEmpty()
            .WithMessage("Name behind is required when uniform color is selected.")
            .When(v => v.IsUniformColorSelected);
    }
}

