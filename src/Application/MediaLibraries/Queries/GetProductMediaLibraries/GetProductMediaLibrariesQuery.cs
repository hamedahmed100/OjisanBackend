using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Application.MediaLibraries.Common;

namespace OjisanBackend.Application.MediaLibraries.Queries.GetProductMediaLibraries;

public record GetProductMediaLibrariesQuery : IRequest<IReadOnlyCollection<MediaLibraryDto>>
{
    public Guid ProductPublicId { get; init; }
}

public class GetProductMediaLibrariesQueryHandler : IRequestHandler<GetProductMediaLibrariesQuery, IReadOnlyCollection<MediaLibraryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IStorageUrlResolver _urlResolver;

    public GetProductMediaLibrariesQueryHandler(IApplicationDbContext context, IStorageUrlResolver urlResolver)
    {
        _context = context;
        _urlResolver = urlResolver;
    }

    public async Task<IReadOnlyCollection<MediaLibraryDto>> Handle(GetProductMediaLibrariesQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.PublicId == request.ProductPublicId, cancellationToken)
            ?? throw new OjisanBackend.Application.Common.Exceptions.NotFoundException("Product", request.ProductPublicId);

        var libraryIds = await _context.ProductMediaLibraries
            .Where(pm => pm.ProductId == product.Id)
            .Select(pm => pm.MediaLibraryId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (libraryIds.Count == 0)
        {
            return Array.Empty<MediaLibraryDto>();
        }

        var libraries = await _context.MediaLibraries
            .Include(m => m.Images)
            .Where(m => libraryIds.Contains(m.Id))
            .ToListAsync(cancellationToken);

        var result = libraries
            .Select(l => new MediaLibraryDto
            {
                PublicId = l.PublicId,
                Title = l.Title,
                Description = l.Description,
                Created = l.Created,
                Images = l.Images
                    .Select(i => new MediaLibraryImageDto
                    {
                        PublicId = i.PublicId,
                        FilePath = _urlResolver.ToPublicUrl(i.FilePath),
                        OriginalFileName = i.OriginalFileName
                    })
                    .ToList()
            })
            .ToList();

        return result;
    }
}

