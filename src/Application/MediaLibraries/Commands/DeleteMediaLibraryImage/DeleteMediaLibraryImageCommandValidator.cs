using FluentValidation;

namespace OjisanBackend.Application.MediaLibraries.Commands.DeleteMediaLibraryImage;

public class DeleteMediaLibraryImageCommandValidator : AbstractValidator<DeleteMediaLibraryImageCommand>
{
    public DeleteMediaLibraryImageCommandValidator()
    {
        RuleFor(v => v.LibraryPublicId)
            .NotEmpty();

        RuleFor(v => v.ImagePublicId)
            .NotEmpty();
    }
}

