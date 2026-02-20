using OjisanBackend.Application.Pricing.Queries.CalculatePrice;
using Microsoft.AspNetCore.Http.HttpResults;

namespace OjisanBackend.Web.Endpoints;

public class Pricing : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(CalculatePrice, "calculate").RequireAuthorization();
    }

    public async Task<Ok<PriceCalculationResult>> CalculatePrice(ISender sender, CalculatePriceQuery query)
    {
        var result = await sender.Send(query);

        return TypedResults.Ok(result);
    }
}
