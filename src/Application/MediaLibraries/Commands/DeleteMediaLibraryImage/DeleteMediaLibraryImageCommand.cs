using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;

namespace OjisanBackend.Application.MediaLibraries.Commands.DeleteMediaLibraryImage;

public record DeleteMediaLibraryImageCommand : IRequest
{
    public Guid LibraryPublicId { get; init; }

    public Guid ImagePublicId { get; init; }
}

public class DeleteMediaLibraryImageCommandHandler : IRequestHandler<DeleteMediaLibraryImageCommand>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediaLibraryFileService _fileService;

    public DeleteMediaLibraryImageCommandHandler(IApplicationDbContext context, IMediaLibraryFileService fileService)
    {
        _context = context;
        _fileService = fileService;
    }

    public async Task Handle(DeleteMediaLibraryImageCommand request, CancellationToken cancellationToken)
    {
        var library = await _context.MediaLibraries
            .Include(x => x.Images)
            .FirstOrDefaultAsync(x => x.PublicId == request.LibraryPublicId, cancellationToken)
            ?? throw new OjisanBackend.Application.Common.Exceptions.NotFoundException("MediaLibrary", request.LibraryPublicId);

        var image = library.Images.FirstOrDefault(i => i.PublicId == request.ImagePublicId);
        if (image is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException("MediaLibraryImage", request.ImagePublicId);
        }

        await _fileService.DeleteFileAsync(image.FilePath, cancellationToken);

        _context.MediaLibraryImages.Remove(image);

        await _context.SaveChangesAsync(cancellationToken);
    }
}

