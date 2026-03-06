using FluentValidation;

namespace OjisanBackend.Application.Submissions.Commands.SubmitMemberDesign;

public class SubmitMemberDesignCommandValidator : AbstractValidator<SubmitMemberDesignCommand>
{
    private const int MinBadges = 3;
    private const int MaxBadges = 11;

    public SubmitMemberDesignCommandValidator()
    {
        RuleFor(v => v.Badges)
            .Must(b => b.Count >= MinBadges && b.Count <= MaxBadges)
            .WithMessage($"Badge count must be between {MinBadges} and {MaxBadges}.");

        RuleForEach(v => v.Badges).ChildRules(badge =>
        {
            badge.RuleFor(b => b.Comment)
                .NotEmpty()
                .WithMessage("Each badge requires a non-empty comment.");
        });
    }
}
