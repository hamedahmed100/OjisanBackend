using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Application.Products.Queries.GetActiveProducts;

public record ProductBriefDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal BasePrice { get; init; }
    public ProductTypeDto Type { get; init; } = null!;
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
        // Project only columns that translate to SQL; Type is stored as nvarchar, so avoid (int)p.Type in SQL.
        var items = await _context.Products
            .AsNoTracking()
            .Where(p => p.IsActive)
            .Select(p => new { p.PublicId, p.Name, p.BasePrice, TypeName = p.Type.ToString() })
            .ToListAsync(cancellationToken);

        return items.Select(p => new ProductBriefDto
        {
            Id = p.PublicId,
            Name = p.Name,
            BasePrice = p.BasePrice,
            Type = new ProductTypeDto
            {
                Value = (int)Enum.Parse<ProductType>(p.TypeName),
                Name = p.TypeName
            }
        }).ToList();
    }
}
