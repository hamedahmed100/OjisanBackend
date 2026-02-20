using OjisanBackend.Application.Admin.Commands.ReviewSubmission;
using OjisanBackend.Application.Admin.Commands.ToggleEditLock;
using OjisanBackend.Application.Admin.Queries.GetGroupsInReview;
using OjisanBackend.Application.Payments.Commands.RequestSecondPayment;
using OjisanBackend.Domain.Constants;
using Microsoft.AspNetCore.Http.HttpResults;

namespace OjisanBackend.Web.Endpoints;

public class AdminPipeline : EndpointGroupBase
{
    public override string? GroupName => "admin/pipeline";

    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetGroupsInReview, "review")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapPut(ReviewSubmission, "groups/{groupId:guid}/submissions/{submissionId:guid}/review")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapPut(ToggleEditLock, "submissions/{id:guid}/toggle-edit")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapPost(RequestSecondPayment, "groups/{id:guid}/request-final-payment")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
    }

    public async Task<Ok<List<GroupInReviewDto>>> GetGroupsInReview(ISender sender)
    {
        var groups = await sender.Send(new GetGroupsInReviewQuery());

        return TypedResults.Ok(groups);
    }

    public async Task<Results<NoContent, BadRequest>> ReviewSubmission(
        ISender sender,
        Guid groupId,
        Guid submissionId,
        ReviewSubmissionCommand command)
    {
        if (groupId != command.GroupId || submissionId != command.SubmissionId)
        {
            return TypedResults.BadRequest();
        }

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    public async Task<Results<NoContent, BadRequest>> ToggleEditLock(
        ISender sender,
        Guid id,
        ToggleEditLockCommand command)
    {
        if (id != command.SubmissionId)
        {
            return TypedResults.BadRequest();
        }

        await sender.Send(command);

        return TypedResults.NoContent();
    }

    public async Task<Results<Ok<string>, BadRequest>> RequestSecondPayment(
        ISender sender,
        Guid id,
        RequestSecondPaymentCommand command)
    {
        if (id != command.GroupId)
        {
            return TypedResults.BadRequest();
        }

        var checkoutUrl = await sender.Send(command);

        return TypedResults.Ok(checkoutUrl);
    }
}

