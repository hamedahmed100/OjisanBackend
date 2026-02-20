using Ardalis.GuardClauses;
using MediatR;
using OjisanBackend.Application.Common.Interfaces;
using OjisanBackend.Domain.Entities;
using OjisanBackend.Domain.Enums;

namespace OjisanBackend.Application.Products.Commands.CreateProduct;

public record CreateProductCommand : IRequest<Guid>
{
    public string Name { get; init; } = string.Empty;

    public string Description { get; init; } = string.Empty;

    public decimal BasePrice { get; init; }

    public ProductType Type { get; init; }
}

public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, Guid>
{
    private readonly IApplicationDbContext _context;

    public CreateProductCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Guid> Handle(CreateProductCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.Name, nameof(request.Name));
        Guard.Against.NegativeOrZero(request.BasePrice, nameof(request.BasePrice));

        var product = new Product
        {
            Name = request.Name,
            Description = request.Description,
            BasePrice = request.BasePrice,
            Type = request.Type,
            IsActive = true
        };

        _context.Products.Add(product);

        await _context.SaveChangesAsync(cancellationToken);

        return product.PublicId;
    }
}
