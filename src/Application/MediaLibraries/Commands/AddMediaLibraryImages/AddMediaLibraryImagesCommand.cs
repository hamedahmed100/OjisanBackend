using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Application.MediaLibraries.Common;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Application.MediaLibraries.Commands.AddMediaLibraryImages;

public record AddMediaLibraryImagesCommand : IRequest<IReadOnlyCollection<MediaLibraryImageDto>>
{
    public Guid LibraryPublicId { get; init; }

    public IReadOnlyCollection<FileUploadDto> Files { get; init; } = Array.Empty<FileUploadDto>();
}

public class AddMediaLibraryImagesCommandHandler : IRequestHandler<AddMediaLibraryImagesCommand, IReadOnlyCollection<MediaLibraryImageDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediaLibraryFileService _fileService;

    public AddMediaLibraryImagesCommandHandler(IApplicationDbContext context, IMediaLibraryFileService fileService)
    {
        _context = context;
        _fileService = fileService;
    }

    public async Task<IReadOnlyCollection<MediaLibraryImageDto>> Handle(AddMediaLibraryImagesCommand request, CancellationToken cancellationToken)
    {
        var mediaLibrary = await _context.MediaLibraries
            .FirstOrDefaultAsync(x => x.PublicId == request.LibraryPublicId, cancellationToken)
            ?? throw new OjisanBackend.Application.Common.Exceptions.NotFoundException("MediaLibrary", request.LibraryPublicId);

        if (request.Files.Count == 0)
        {
            throw new BadRequestException("At least one image file is required.");
        }

        var result = new List<MediaLibraryImageDto>();

        foreach (var file in request.Files)
        {
            var relativePath = await _fileService.SaveFileAsync(
                file.Content,
                file.FileName,
                file.ContentType,
                cancellationToken);

            var image = new MediaLibraryImage
            {
                FilePath = relativePath,
                OriginalFileName = file.FileName
            };

            mediaLibrary.AddImage(image);

            result.Add(new MediaLibraryImageDto
            {
                PublicId = image.PublicId,
                FilePath = image.FilePath,
                OriginalFileName = image.OriginalFileName
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return result;
    }
}

