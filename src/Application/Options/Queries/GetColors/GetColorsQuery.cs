using MediatR;
using Microsoft.EntityFrameworkCore;
using OjisanBackend.Application.Common.Interfaces;

namespace OjisanBackend.Application.Options.Queries.GetColors;

/// <summary>
/// Color option for jacket customization.
/// </summary>
public record ColorOptionDto
{
    public int Id { get; init; }
    public string NameAr { get; init; } = string.Empty;
    public string NameEn { get; init; } = string.Empty;
    public string HexCode { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
}

/// <summary>
/// Colors grouped by type (Jacket, Sleeve, Elastic).
/// </summary>
public record ColorsGroupedByTypeDto
{
    public string Type { get; init; } = string.Empty;
    public List<ColorOptionDto> Colors { get; init; } = new();
}

public record GetColorsQuery : IRequest<ColorsGroupedByTypeDto[]>;

public class GetColorsQueryHandler : IRequestHandler<GetColorsQuery, ColorsGroupedByTypeDto[]>
{
    private readonly IApplicationDbContext _context;

    public GetColorsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<ColorsGroupedByTypeDto[]> Handle(GetColorsQuery request, CancellationToken cancellationToken)
    {
        var all = await _context.ProductColors
            .AsNoTracking()
            .OrderBy(pc => pc.Type)
            .ThenBy(pc => pc.NameEn)
            .Select(pc => new ColorOptionDto
            {
                Id = pc.Id,
                NameAr = pc.NameAr,
                NameEn = pc.NameEn,
                HexCode = pc.HexCode,
                Type = pc.Type.ToString()
            })
            .ToListAsync(cancellationToken);

        return all
            .GroupBy(c => c.Type)
            .OrderBy(g => g.Key)
            .Select(g => new ColorsGroupedByTypeDto { Type = g.Key, Colors = g.ToList() })
            .ToArray();
    }
}
