using Ardalis.GuardClauses;
using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;

namespace OjisanBackend.Application.Products.Queries.GetProductDetails;

public record ProductDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Description { get; init; } = string.Empty;
    public decimal BasePrice { get; init; }
    public ProductTypeDto Type { get; init; } = null!;
    public bool IsActive { get; init; }
    public List<ProductOptionDto> Options { get; init; } = new();
    public List<BadgePositionDto> BadgePositions { get; init; } = new();
}

public record ProductTypeDto
{
    public int Value { get; init; }
    public string Name { get; init; } = string.Empty;
}

public record ProductOptionDto
{
    public Guid Id { get; init; }
    public OptionCategoryDto Category { get; init; } = null!;
    public string Value { get; init; } = string.Empty;
    public decimal AdditionalCost { get; init; }
}

public record OptionCategoryDto
{
    public int Value { get; init; }
    public string Name { get; init; } = string.Empty;
}

public record BadgePositionDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool IsRequired { get; init; }
}

public record GetProductDetailsQuery : IRequest<ProductDto>
{
    public Guid ProductId { get; init; }
}

public class GetProductDetailsQueryHandler : IRequestHandler<GetProductDetailsQuery, ProductDto>
{
    private readonly IApplicationDbContext _context;

    public GetProductDetailsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ProductDto> Handle(GetProductDetailsQuery request, CancellationToken cancellationToken)
    {
        var product = await _context.Products
            .AsNoTracking()
            .Include(p => p.Options)
            .Include(p => p.BadgePositions)
            .FirstOrDefaultAsync(p => p.PublicId == request.ProductId, cancellationToken);

        if (product is null)
        {
            throw new OjisanBackend.Application.Common.Exceptions.NotFoundException(nameof(Product), request.ProductId);
        }

        return new ProductDto
        {
            Id = product.PublicId,
            Name = product.Name,
            Description = product.Description,
            BasePrice = product.BasePrice,
            Type = new ProductTypeDto
            {
                Value = (int)product.Type,
                Name = product.Type.ToString()
            },
            IsActive = product.IsActive,
            Options = product.Options.Select(o => new ProductOptionDto
            {
                Id = o.PublicId,
                Category = new OptionCategoryDto
                {
                    Value = (int)o.Category,
                    Name = o.Category.ToString()
                },
                Value = o.Value,
                AdditionalCost = o.AdditionalCost
            }).ToList(),
            BadgePositions = product.BadgePositions.Select(bp => new BadgePositionDto
            {
                Id = bp.PublicId,
                Name = bp.Name,
                IsRequired = bp.IsRequired
            }).ToList()
        };
    }
}
