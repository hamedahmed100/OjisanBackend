using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using OjisanBackend.Application.Submissions.Commands.SubmitSingleOrder;

namespace OjisanBackend.Web.Endpoints;

public class Submissions : EndpointGroupBase
{
    public override string? GroupName => "submissions";

    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(SubmitSingleOrder, "single")
            .RequireAuthorization();
    }

    public async Task<Created<Guid>> SubmitSingleOrder(ISender sender, SubmitSingleOrderCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/api/submissions/single/{id}", id);
    }
}

