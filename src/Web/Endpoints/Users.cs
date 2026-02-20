using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using OjisanBackend.Application.Users.Commands.UpdateUserAddress;
using OjisanBackend.Infrastructure.Identity;

namespace OjisanBackend.Web.Endpoints;

public class Users : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapIdentityApi<ApplicationUser>();

        groupBuilder.MapPut(UpdateUserAddress, "address")
            .RequireAuthorization();
    }

    public async Task<NoContent> UpdateUserAddress(ISender sender, UpdateUserAddressCommand command)
    {
        await sender.Send(command);

        return TypedResults.NoContent();
    }
}
