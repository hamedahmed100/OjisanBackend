using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using OjisanBackend.Application.Orders.Commands.CreateJacketOrder;
using OjisanBackend.Application.Orders.Queries.GetMyOrders;

namespace OjisanBackend.Web.Endpoints;

public class Orders : EndpointGroupBase
{
    public override string? GroupName => "orders";

    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost("/jacket", CreateJacketOrder)
            .RequireAuthorization()
            .WithName("CreateJacketOrder")
            .WithSummary("Create a single jacket order (badges, add-ons, design).")
            .Produces<CreateJacketOrderResult>(201)
            .ProducesProblem(400);

        groupBuilder.MapGet(GetMyOrders, "mine")
            .RequireAuthorization()
            .WithName("GetMyOrders")
            .WithSummary("Get the current user's orders (submissions) with full details: badges, add-ons, design JSON.")
            .Produces<List<MyOrderDto>>(200);
    }

    public async Task<Results<Created<CreateJacketOrderResult>, ProblemHttpResult>> CreateJacketOrder(
        ISender sender,
        CreateJacketOrderCommand command)
    {
        var result = await sender.Send(command);
        return TypedResults.Created($"/api/orders/jacket/{result.OrderId}", result);
    }

    public async Task<Ok<List<MyOrderDto>>> GetMyOrders(ISender sender)
    {
        var orders = await sender.Send(new GetMyOrdersQuery());
        return TypedResults.Ok(orders);
    }
}
