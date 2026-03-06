using FluentValidation;

namespace OjisanBackend.Application.Orders.Commands.CreateJacketOrder;

public class CreateJacketOrderCommandValidator : AbstractValidator<CreateJacketOrderCommand>
{
    private const int MinBadges = 3;
    private const int MaxBadges = 11;

    public CreateJacketOrderCommandValidator()
    {
        RuleFor(x => x.Badges)
            .Must(b => b.Count >= MinBadges && b.Count <= MaxBadges)
            .WithMessage($"Badge count must be between {MinBadges} and {MaxBadges}.");

        RuleForEach(x => x.Badges).ChildRules(badge =>
        {
            badge.RuleFor(b => b.ImageUrl).NotEmpty().WithMessage("Badge image URL is required.");
            badge.RuleFor(b => b.Comment).NotEmpty().WithMessage("Comment is mandatory for each badge.");
        });
    }
}
