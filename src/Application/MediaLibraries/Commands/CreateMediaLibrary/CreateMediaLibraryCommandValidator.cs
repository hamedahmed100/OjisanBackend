using FluentValidation;
using OjisanBackend.Application.MediaLibraries.Common;

namespace OjisanBackend.Application.MediaLibraries.Commands.CreateMediaLibrary;

public class CreateMediaLibraryCommandValidator : AbstractValidator<CreateMediaLibraryCommand>
{
    private const long MaxFileSize = 5 * 1024 * 1024; // 5 MB

    public CreateMediaLibraryCommandValidator()
    {
        RuleFor(v => v.Title)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(v => v.Description)
            .MaximumLength(2000);

        RuleFor(v => v.Files)
            .NotEmpty()
            .WithMessage("At least one image file is required.");

        RuleForEach(v => v.Files)
            .SetValidator(new FileUploadDtoValidator());
    }

    private class FileUploadDtoValidator : AbstractValidator<FileUploadDto>
    {
        public FileUploadDtoValidator()
        {
            RuleFor(f => f.Content)
                .NotNull()
                .Must(stream => stream != null && stream.Length <= MaxFileSize)
                .WithMessage($"File size must not exceed {MaxFileSize / (1024 * 1024)} MB.");

            RuleFor(f => f.ContentType)
                .NotEmpty()
                .Must(t => t == "image/png" || t == "image/jpeg")
                .WithMessage("Only PNG and JPEG image types are supported.");

            RuleFor(f => f.FileName)
                .NotEmpty();
        }
    }
}

