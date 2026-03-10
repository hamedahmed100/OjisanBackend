using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Application.MediaLibraries.Common;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Application.MediaLibraries.Commands.CreateMediaLibrary;

public record CreateMediaLibraryCommand : IRequest<CreateMediaLibraryResult>
{
    public string Title { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public IReadOnlyCollection<Guid> ProductPublicIds { get; init; } = Array.Empty<Guid>();

    public IReadOnlyCollection<FileUploadDto> Files { get; init; } = Array.Empty<FileUploadDto>();
}

public class CreateMediaLibraryCommandHandler : IRequestHandler<CreateMediaLibraryCommand, CreateMediaLibraryResult>
{
    private readonly IApplicationDbContext _context;
    private readonly IMediaLibraryFileService _fileService;

    public CreateMediaLibraryCommandHandler(IApplicationDbContext context, IMediaLibraryFileService fileService)
    {
        _context = context;
        _fileService = fileService;
    }

    public async Task<CreateMediaLibraryResult> Handle(CreateMediaLibraryCommand request, CancellationToken cancellationToken)
    {
        if (request.Files.Count == 0)
        {
            throw new BadRequestException("At least one image file is required to create a media library.");
        }

        var mediaLibrary = new MediaLibrary
        {
            Title = request.Title,
            Description = request.Description
        };

        _context.MediaLibraries.Add(mediaLibrary);

        // Resolve products by public ID
        if (request.ProductPublicIds.Count > 0)
        {
            var products = await _context.Products
                .Where(p => request.ProductPublicIds.Contains(p.PublicId))
                .ToListAsync(cancellationToken);

            if (products.Count != request.ProductPublicIds.Count)
            {
                var foundIds = products.Select(p => p.PublicId).ToHashSet();
                var missing = request.ProductPublicIds.First(id => !foundIds.Contains(id));
                throw new OjisanBackend.Application.Common.Exceptions.NotFoundException("Product", missing);
            }

            foreach (var product in products)
            {
                _context.ProductMediaLibraries.Add(new ProductMediaLibrary
                {
                    ProductId = product.Id,
                    MediaLibrary = mediaLibrary
                });
            }
        }

        var imageDtos = new List<MediaLibraryImageDto>();

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

            imageDtos.Add(new MediaLibraryImageDto
            {
                PublicId = image.PublicId,
                FilePath = image.FilePath,
                OriginalFileName = image.OriginalFileName
            });
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new CreateMediaLibraryResult
        {
            PublicId = mediaLibrary.PublicId,
            Title = mediaLibrary.Title,
            Images = imageDtos
        };
    }
}

