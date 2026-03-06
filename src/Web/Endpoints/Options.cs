using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using OjisanBackend.Application.Options.Queries.GetAddOns;
using OjisanBackend.Application.Options.Queries.GetColors;

namespace OjisanBackend.Web.Endpoints;

public class Options : EndpointGroupBase
{
    public override string? GroupName => "options";

    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet("/colors", GetColors).AllowAnonymous();
        groupBuilder.MapGet("/add-ons", GetAddOns).AllowAnonymous();
    }

    public async Task<Ok<ColorsGroupedByTypeDto[]>> GetColors(ISender sender)
    {
        var result = await sender.Send(new GetColorsQuery());
        return TypedResults.Ok(result);
    }

    public async Task<Ok<List<AddOnOptionDto>>> GetAddOns(ISender sender)
    {
        var result = await sender.Send(new GetAddOnsQuery());
        return TypedResults.Ok(result);
    }
}
