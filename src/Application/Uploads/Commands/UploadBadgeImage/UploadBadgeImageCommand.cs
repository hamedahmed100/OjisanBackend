using Ardalis.GuardClauses;
using MediatR;
using OjisanBackend.Application.Common.Interfaces;

namespace OjisanBackend.Application.Uploads.Commands.UploadBadgeImage;

public record UploadBadgeImageCommand : IRequest<string>
{
    public Stream Content { get; init; } = null!;

    public string FileName { get; init; } = string.Empty;

    public string ContentType { get; init; } = string.Empty;
}

public class UploadBadgeImageCommandHandler : IRequestHandler<UploadBadgeImageCommand, string>
{
    private readonly IImageUploadService _imageUploadService;

    public UploadBadgeImageCommandHandler(IImageUploadService imageUploadService)
    {
        _imageUploadService = imageUploadService;
    }

    public async Task<string> Handle(UploadBadgeImageCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request.Content, nameof(request.Content));
        Guard.Against.NullOrWhiteSpace(request.FileName, nameof(request.FileName));
        Guard.Against.NullOrWhiteSpace(request.ContentType, nameof(request.ContentType));

        var imageUrl = await _imageUploadService.UploadImageAsync(
            request.Content,
            request.FileName,
            request.ContentType,
            cancellationToken);

        return imageUrl;
    }
}
