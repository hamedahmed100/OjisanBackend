using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;

namespace OjisanBackend.Application.Products.Queries.GetActiveProducts;

public record ProductBriefDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal BasePrice { get; init; }
    public ProductTypeDto Type { get; init; }
}

public record ProductTypeDto
{
    public int Value { get; init; }
    public string Name { get; init; } = string.Empty;
}

public record GetActiveProductsQuery : IRequest<List<ProductBriefDto>>;

public class GetActiveProductsQueryHandler : IRequestHandler<GetActiveProductsQuery, List<ProductBriefDto>>
{
    private readonly IApplicationDbContext _context;

    public GetActiveProductsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<ProductBriefDto>> Handle(GetActiveProductsQuery request, CancellationToken cancellationToken)
    {
        return await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new ProductBriefDto
            {
                Id = p.PublicId,
                Name = p.Name,
                BasePrice = p.BasePrice,
                Type = new ProductTypeDto
                {
                    Value = (int)p.Type,
                    Name = p.Type.ToString()
                }
            })
            .ToListAsync(cancellationToken);
    }
}
