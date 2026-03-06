using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;

namespace OjisanBackend.Application.Options.Queries.GetAddOns;

/// <summary>
/// Paid add-on option for jacket.
/// </summary>
public record AddOnOptionDto
{
    public Guid Id { get; init; }
    public string NameAr { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public decimal Price { get; init; }
}

public record GetAddOnsQuery : IRequest<List<AddOnOptionDto>>;

public class GetAddOnsQueryHandler : IRequestHandler<GetAddOnsQuery, List<AddOnOptionDto>>
{
    private readonly IApplicationDbContext _context;

    public GetAddOnsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<AddOnOptionDto>> Handle(GetAddOnsQuery request, CancellationToken cancellationToken)
    {
        return await _context.ProductAddOns
            .AsNoTracking()
            .OrderBy(pa => pa.NameEn)
            .Select(pa => new AddOnOptionDto
            {
                Id = pa.PublicId,
                NameAr = pa.NameAr,
                NameEn = pa.NameEn,
                Price = pa.Price
            })
            .ToListAsync(cancellationToken);
    }
}
