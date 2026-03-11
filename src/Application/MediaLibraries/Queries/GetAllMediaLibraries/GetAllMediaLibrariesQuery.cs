using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Application.MediaLibraries.Common;

namespace OjisanBackend.Application.MediaLibraries.Queries.GetAllMediaLibraries;

public record GetAllMediaLibrariesQuery : IRequest<IReadOnlyCollection<AdminMediaLibraryDto>>;

public class GetAllMediaLibrariesQueryHandler : IRequestHandler<GetAllMediaLibrariesQuery, IReadOnlyCollection<AdminMediaLibraryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly IStorageUrlResolver _urlResolver;

    public GetAllMediaLibrariesQueryHandler(IApplicationDbContext context, IStorageUrlResolver urlResolver)
    {
        _context = context;
        _urlResolver = urlResolver;
    }

    public async Task<IReadOnlyCollection<AdminMediaLibraryDto>> Handle(GetAllMediaLibrariesQuery request, CancellationToken cancellationToken)
    {
        var libraries = await _context.MediaLibraries
            .Include(m => m.Images)
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (libraries.Count == 0)
        {
            return Array.Empty<AdminMediaLibraryDto>();
        }

        var libraryIds = libraries.Select(l => l.Id).ToList();

        var productLinks = await _context.ProductMediaLibraries
            .Where(pm => libraryIds.Contains(pm.MediaLibraryId))
            .Join(
                _context.Products.AsNoTracking(),
                pm => pm.ProductId,
                p => p.Id,
                (pm, p) => new { pm.MediaLibraryId, Product = p })
            .ToListAsync(cancellationToken);

        var productsByLibrary = productLinks
            .GroupBy(x => x.MediaLibraryId)
            .ToDictionary(
                g => g.Key,
                g => g.Select(x => new AdminMediaLibraryProductDto
                {
                    PublicId = x.Product.PublicId,
                    Name = x.Product.Name
                }).ToList());

        var result = libraries
            .Select(l =>
            {
                var allImages = l.Images
                    .Select(i => new MediaLibraryImageDto
                    {
                        PublicId = i.PublicId,
                        FilePath = _urlResolver.ToPublicUrl(i.FilePath),
                        OriginalFileName = i.OriginalFileName
                    })
                    .ToList();

                return new AdminMediaLibraryDto
                {
                    PublicId = l.PublicId,
                    Title = l.Title,
                    Description = l.Description,
                    Created = l.Created,
                    Products = productsByLibrary.TryGetValue(l.Id, out var products)
                        ? products
                        : Array.Empty<AdminMediaLibraryProductDto>(),
                    TotalImageCount = allImages.Count,
                    Images = allImages.Take(5).ToList()
                };
            })
            .ToList();

        return result;
    }
}

