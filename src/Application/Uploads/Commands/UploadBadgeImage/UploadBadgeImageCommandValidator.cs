using FluentValidation;

namespace OjisanBackend.Application.Uploads.Commands.UploadBadgeImage;

public class UploadBadgeImageCommandValidator : AbstractValidator<UploadBadgeImageCommand>
{
    private const long MaxFileSize = 5 * 1024 * 1024; // 5MB

    public UploadBadgeImageCommandValidator()
    {
        RuleFor(v => v.Content)
            .NotNull()
            .WithMessage("ملف الصورة مطلوب.")
            .Must(stream => stream != null && stream.Length > 0)
            .WithMessage("ملف الصورة فارغ.")
            .Must(stream => stream != null && stream.Length <= MaxFileSize)
            .WithMessage($"حجم الملف يجب ألا يتجاوز {MaxFileSize / (1024 * 1024)} ميجابايت.");

        RuleFor(v => v.ContentType)
            .NotEmpty()
            .WithMessage("نوع الملف مطلوب.")
            .Must(contentType => contentType == "image/png" || contentType == "image/jpeg")
            .WithMessage("نوع الملف المدعوم هو PNG أو JPEG فقط.");

        RuleFor(v => v.FileName)
            .NotEmpty()
            .WithMessage("اسم الملف مطلوب.");
    }
}
