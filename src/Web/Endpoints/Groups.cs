using OjisanBackend.Application.Groups.Commands.CreateGroup;
using OjisanBackend.Application.Groups.Commands.JoinGroup;
using OjisanBackend.Application.Groups.Queries.GetGroupInviteLink;
using OjisanBackend.Application.Groups.Queries.GetGroupByInviteCode;
using OjisanBackend.Application.Submissions.Commands.SubmitMemberDesign;
using OjisanBackend.Application.Submissions.Commands.UpdateSubmission;
using Microsoft.AspNetCore.Http.HttpResults;

namespace OjisanBackend.Web.Endpoints;

public class Groups : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(CreateGroup).RequireAuthorization();
        groupBuilder.MapGet(GetGroupInviteLink, "{id:guid}/invite-link").RequireAuthorization();
        groupBuilder.MapGet(GetGroupByInviteCode, "invite/{code}").AllowAnonymous().RequireRateLimiting("AnonymousPolicy");
        groupBuilder.MapPost(JoinGroup, "invite/{code}/join").RequireAuthorization();
        groupBuilder.MapPost(SubmitMemberDesign, "{id:guid}/submissions").RequireAuthorization();
        groupBuilder.MapPut(UpdateSubmission, "{groupId:guid}/submissions/{submissionId:guid}").RequireAuthorization();
    }

    public async Task<Created<Guid>> CreateGroup(ISender sender, CreateGroupCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/{nameof(Groups)}/{id}", id);
    }

    public async Task<Results<Ok<string>, NotFound>> GetGroupInviteLink(ISender sender, Guid id)
    {
        var inviteLink = await sender.Send(new GetGroupInviteLinkQuery { GroupId = id });

        if (string.IsNullOrWhiteSpace(inviteLink))
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(inviteLink);
    }

    public async Task<Results<Ok<GroupInviteInfoDto>, NotFound>> GetGroupByInviteCode(ISender sender, string code)
    {
        var groupInfo = await sender.Send(new GetGroupByInviteCodeQuery { InviteCode = code });

        if (groupInfo == null)
        {
            return TypedResults.NotFound();
        }

        return TypedResults.Ok(groupInfo);
    }

    public async Task<NoContent> JoinGroup(ISender sender, string code)
    {
        await sender.Send(new JoinGroupCommand { InviteCode = code });

        return TypedResults.NoContent();
    }

    public async Task<Results<Created<Guid>, BadRequest>> SubmitMemberDesign(
        ISender sender,
        Guid id,
        SubmitMemberDesignCommand command)
    {
        if (id != command.GroupId)
        {
            return TypedResults.BadRequest();
        }

        var submissionId = await sender.Send(command);

        return TypedResults.Created($"/{nameof(Groups)}/{id}/submissions/{submissionId}", submissionId);
    }

    public async Task<Results<NoContent, BadRequest>> UpdateSubmission(
        ISender sender,
        Guid groupId,
        Guid submissionId,
        UpdateSubmissionCommand command)
    {
        if (groupId != command.GroupId || submissionId != command.SubmissionId)
        {
            return TypedResults.BadRequest();
        }

        await sender.Send(command);

        return TypedResults.NoContent();
    }
}

