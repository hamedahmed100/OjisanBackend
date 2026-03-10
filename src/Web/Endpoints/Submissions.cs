using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using OjisanBackend.Application.Submissions.Commands.ResubmitSingleOrder;
using OjisanBackend.Application.Submissions.Commands.SubmitSingleOrder;

namespace OjisanBackend.Web.Endpoints;

public class Submissions : EndpointGroupBase
{
    public override string? GroupName => "submissions";

    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(SubmitSingleOrder, "single")
            .RequireAuthorization();

        groupBuilder.MapPut(ResubmitSingleOrder, "{submissionId:guid}/resubmit")
            .RequireAuthorization();
    }

    public async Task<Created<Guid>> SubmitSingleOrder(ISender sender, SubmitSingleOrderCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/api/submissions/single/{id}", id);
    }

    public async Task<Results<NoContent, BadRequest>> ResubmitSingleOrder(
        ISender sender,
        Guid submissionId,
        ResubmitSingleOrderCommand command)
    {
        if (submissionId != command.SubmissionId)
        {
            return TypedResults.BadRequest();
        }

        await sender.Send(command);
        return TypedResults.NoContent();
    }
}

