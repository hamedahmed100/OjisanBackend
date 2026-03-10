using OjisanBackend.Application.Admin.Commands.AcceptSingleSubmission;
using OjisanBackend.Application.Admin.Commands.MarkGroupAsShipped;
using OjisanBackend.Application.Admin.Commands.MarkSubmissionAsShipped;
using OjisanBackend.Application.Admin.Commands.RejectSingleSubmission;
using OjisanBackend.Application.Admin.Commands.ReviewGroupBatch;
using OjisanBackend.Application.Admin.Commands.ReviewSubmission;
using OjisanBackend.Application.Admin.Commands.ToggleEditLock;
using OjisanBackend.Application.Admin.Queries.GetAllSubmissionsUnderReview;
using OjisanBackend.Application.Admin.Queries.GetGroupPreview;
using OjisanBackend.Application.Admin.Queries.GetSubmissionPreview;
using OjisanBackend.Application.Payments.Commands.RequestSecondPayment;
using OjisanBackend.Domain.Constants;
using Microsoft.AspNetCore.Http.HttpResults;

namespace OjisanBackend.Web.Endpoints;

public class AdminPipeline : EndpointGroupBase
{
    public override string? GroupName => "admin/pipeline";

    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetAllSubmissionsUnderReview, "review")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapGet(GetSubmissionPreview, "submissions/{submissionId:guid}/preview")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapGet(GetGroupPreview, "groups/{groupId:guid}/preview")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapPut(ReviewSubmission, "groups/{groupId:guid}/submissions/{submissionId:guid}/review")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapPut(ReviewGroupBatch, "groups/{groupId:guid}/review-batch")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapPut(AcceptSingleSubmission, "submissions/{submissionId:guid}/accept")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapPut(RejectSingleSubmission, "submissions/{submissionId:guid}/reject")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapPut(ToggleEditLock, "submissions/{id:guid}/toggle-edit")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapPost(RequestSecondPayment, "groups/{id:guid}/request-final-payment")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapPut(MarkGroupAsShipped, "groups/{id:guid}/mark-shipped")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapPut(MarkSubmissionAsShipped, "submissions/{id:guid}/mark-shipped")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
    }

    public async Task<Ok<AdminKanbanBoardResponse>> GetAllSubmissionsUnderReview(ISender sender)
    {
        var result = await sender.Send(new GetAllSubmissionsUnderReviewQuery());
        return TypedResults.Ok(result);
    }

    public async Task<Ok<GetGroupPreviewResponse>> GetGroupPreview(ISender sender, Guid groupId)
    {
        var result = await sender.Send(new GetGroupPreviewQuery(groupId));
        return TypedResults.Ok(result);
    }

    public async Task<Ok<GetSubmissionPreviewResponse>> GetSubmissionPreview(ISender sender, Guid submissionId)
    {
        var result = await sender.Send(new GetSubmissionPreviewQuery(submissionId));
        return TypedResults.Ok(result);
    }

    public async Task<Results<NoContent, BadRequest>> AcceptSingleSubmission(ISender sender, Guid submissionId)
    {
        await sender.Send(new AcceptSingleSubmissionCommand { SubmissionId = submissionId });
        return TypedResults.NoContent();
    }

    public async Task<Results<NoContent, BadRequest>> RejectSingleSubmission(
        ISender sender,
        Guid submissionId,
        RejectSingleSubmissionCommand command)
    {
        if (submissionId != command.SubmissionId)
        {
            return TypedResults.BadRequest();
        }

        await sender.Send(command);
        return TypedResults.NoContent();
    }

    public async Task<Results<NoContent, BadRequest>> ReviewGroupBatch(
        ISender sender,
        Guid groupId,
        ReviewGroupBatchCommand command)
    {
        if (groupId != command.GroupId)
        {
            return TypedResults.BadRequest();
        }

        await sender.Send(command);
        return TypedResults.NoContent();
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

    public async Task<Results<NoContent, BadRequest>> MarkGroupAsShipped(
        ISender sender,
        Guid id)
    {
        await sender.Send(new MarkGroupAsShippedCommand { GroupId = id });
        return TypedResults.NoContent();
    }

    public async Task<Results<NoContent, BadRequest>> MarkSubmissionAsShipped(
        ISender sender,
        Guid id)
    {
        await sender.Send(new MarkSubmissionAsShippedCommand { SubmissionId = id });
        return TypedResults.NoContent();
    }
}

