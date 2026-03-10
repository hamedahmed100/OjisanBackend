using FluentValidation;

namespace OjisanBackend.Application.Submissions.Commands.SubmitMemberDesign;

public class SubmitMemberDesignCommandValidator : AbstractValidator<SubmitMemberDesignCommand>
{
    private const int MinBadges = 3;
    private const int MaxBadges = 12;

    public SubmitMemberDesignCommandValidator()
    {
        RuleFor(v => v.Badges)
            .Must(b => b.Count >= MinBadges && b.Count <= MaxBadges)
            .WithMessage($"Badge count must be between {MinBadges} and {MaxBadges}.");
    }
}
