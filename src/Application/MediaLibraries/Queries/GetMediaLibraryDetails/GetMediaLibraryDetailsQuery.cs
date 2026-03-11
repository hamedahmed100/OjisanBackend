using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Application.MediaLibraries.Common;

namespace OjisanBackend.Application.MediaLibraries.Queries.GetMediaLibraryDetails;

public record GetMediaLibraryDetailsQuery : IRequest<AdminMediaLibraryDto>
{
    public Guid LibraryPublicId { get; init; }
}

public class GetMediaLibraryDetailsQueryHandler : IRequestHandler<GetMediaLibraryDetailsQuery, AdminMediaLibraryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly IStorageUrlResolver _urlResolver;

    public GetMediaLibraryDetailsQueryHandler(IApplicationDbContext context, IStorageUrlResolver urlResolver)
    {
        _context = context;
        _urlResolver = urlResolver;
    }

    public async Task<AdminMediaLibraryDto> Handle(GetMediaLibraryDetailsQuery request, CancellationToken cancellationToken)
    {
        var library = await _context.MediaLibraries
            .Include(m => m.Images)
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.PublicId == request.LibraryPublicId, cancellationToken)
            ?? throw new OjisanBackend.Application.Common.Exceptions.NotFoundException("MediaLibrary", request.LibraryPublicId);

        var productLinks = await _context.ProductMediaLibraries
            .Where(pm => pm.MediaLibraryId == library.Id)
            .Join(
                _context.Products.AsNoTracking(),
                pm => pm.ProductId,
                p => p.Id,
                (pm, p) => p)
            .ToListAsync(cancellationToken);

        var products = productLinks
            .Select(p => new AdminMediaLibraryProductDto
            {
                PublicId = p.PublicId,
                Name = p.Name
            })
            .ToList();

        var images = library.Images
            .Select(i => new MediaLibraryImageDto
            {
                PublicId = i.PublicId,
                FilePath = _urlResolver.ToPublicUrl(i.FilePath),
                OriginalFileName = i.OriginalFileName
            })
            .ToList();

        return new AdminMediaLibraryDto
        {
            PublicId = library.PublicId,
            Title = library.Title,
            Description = library.Description,
            Created = library.Created,
            Products = products,
            TotalImageCount = images.Count,
            Images = images
        };
    }
}

