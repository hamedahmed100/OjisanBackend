using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using OjisanBackend.Application.Groups.Commands.CreateGroup;
using OjisanBackend.Application.Groups.Commands.JoinGroup;
using OjisanBackend.Application.Groups.Queries.GetGroupByInviteCode;
using OjisanBackend.Application.Groups.Queries.GetGroupDetails;
using OjisanBackend.Application.Groups.Queries.GetGroupDiscountEligibility;
using OjisanBackend.Application.Groups.Queries.GetGroupInviteLink;
using OjisanBackend.Application.Groups.Queries.GetMyGroups;
using OjisanBackend.Application.Groups.Queries.ValidatePromotion;
using OjisanBackend.Application.Submissions.Commands.SubmitMemberDesign;
using OjisanBackend.Application.Submissions.Commands.UpdateSubmission;

namespace OjisanBackend.Web.Endpoints;

public class Groups : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(CreateGroup)
            .RequireAuthorization()
            .WithName("CreateGroup")
            .WithSummary("Create a new group with name, member count, and optional uniform-colour discount.")
            .Produces<CreateGroupResult>(201)
            .ProducesProblem(400);

        groupBuilder.MapGet(ValidatePromotion, "promotions/validate")
            .AllowAnonymous()
            .WithName("ValidatePromotion")
            .WithSummary("Check if the uniform-colour promotion is active for a given member count (e.g. 6+).")
            .Produces<ValidatePromotionResult>(200);

        groupBuilder.MapGet(GetGroupDiscountEligibility, "discount-eligibility")
            .AllowAnonymous()
            .WithName("GetGroupDiscountEligibility")
            .WithSummary("Check if product + member count + uniform colour are eligible for a discount, and get the full price breakdown.")
            .Produces<GroupDiscountEligibilityResult>(200)
            .ProducesProblem(404);

        groupBuilder.MapGet(GetMyGroups, "mine")
            .RequireAuthorization()
            .WithName("GetMyGroups")
            .WithSummary("Get all groups the current user created or joined, with role (Leader/Member) and progress.")
            .Produces<List<MyGroupDto>>(200);

        groupBuilder.MapGet(GetGroupDetails, "{id:guid}")
            .RequireAuthorization()
            .WithName("GetGroupDetails")
            .WithSummary("Get full group details: invite link/code, members, submissions (badges, comments), progress (X out of Y).")
            .Produces<GetGroupDetailsResult>(200)
            .ProducesProblem(404)
            .ProducesProblem(403);
        groupBuilder.MapGet(GetGroupInviteLink, "{id:guid}/invite-link")
            .RequireAuthorization()
            .WithName("GetGroupInviteLink")
            .WithSummary("Get the invite link for a group.");
        groupBuilder.MapGet(GetGroupByInviteCode, "invite/{code}")
            .AllowAnonymous()
            .RequireRateLimiting("AnonymousPolicy")
            .WithName("GetGroupByInviteCode")
            .WithSummary("Get group info by invite code.");
        groupBuilder.MapPost(JoinGroup, "invite/{code}/join")
            .RequireAuthorization()
            .WithName("JoinGroup")
            .WithSummary("Join a group using an invite code.");
        groupBuilder.MapPost(SubmitMemberDesign, "{id:guid}/submissions")
            .RequireAuthorization()
            .WithName("SubmitMemberDesign")
            .WithSummary("Submit a member design for a group.");
        groupBuilder.MapPut(UpdateSubmission, "{groupId:guid}/submissions/{submissionId:guid}")
            .RequireAuthorization()
            .WithName("UpdateSubmission")
            .WithSummary("Update a submission (e.g. after rejection).");
    }

    public async Task<Created<CreateGroupResult>> CreateGroup(ISender sender, CreateGroupCommand command)
    {
        var result = await sender.Send(command);
        return TypedResults.Created($"/api/Groups/{result.GroupId}", result);
    }

    public async Task<Ok<ValidatePromotionResult>> ValidatePromotion(ISender sender, [AsParameters] ValidatePromotionQuery query)
    {
        var result = await sender.Send(query);
        return TypedResults.Ok(result);
    }

    public async Task<Results<Ok<GroupDiscountEligibilityResult>, NotFound>> GetGroupDiscountEligibility(
        ISender sender,
        Guid productPublicId,
        int memberCount,
        bool isUniformColorSelected,
        [FromQuery] string? addOnIds = null)
    {
        try
        {
            var parsed = ParseGuidList(addOnIds);
            var query = new GetGroupDiscountEligibilityQuery
            {
                ProductPublicId = productPublicId,
                MemberCount = memberCount,
                IsUniformColorSelected = isUniformColorSelected,
                AddOnIds = parsed
            };
            var result = await sender.Send(query);
            return TypedResults.Ok(result);
        }
        catch (OjisanBackend.Application.Common.Exceptions.NotFoundException)
        {
            return TypedResults.NotFound();
        }
    }

    private static List<Guid> ParseGuidList(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return new List<Guid>();
        return value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(s => Guid.TryParse(s, out var g) ? g : (Guid?)null)
            .Where(g => g.HasValue)
            .Select(g => g!.Value)
            .ToList();
    }

    public async Task<Ok<List<MyGroupDto>>> GetMyGroups(ISender sender)
    {
        var groups = await sender.Send(new GetMyGroupsQuery());
        return TypedResults.Ok(groups);
    }

    public async Task<Results<Ok<GetGroupDetailsResult>, NotFound, ForbidHttpResult>> GetGroupDetails(ISender sender, Guid id)
    {
        try
        {
            var result = await sender.Send(new GetGroupDetailsQuery { GroupId = id });
            return TypedResults.Ok(result);
        }
        catch (OjisanBackend.Application.Common.Exceptions.NotFoundException)
        {
            return TypedResults.NotFound();
        }
        catch (OjisanBackend.Application.Common.Exceptions.ForbiddenAccessException)
        {
            return TypedResults.Forbid();
        }
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

