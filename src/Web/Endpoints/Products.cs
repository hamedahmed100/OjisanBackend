using OjisanBackend.Application.Products.Commands.CreateProduct;
using OjisanBackend.Application.Products.Queries.GetActiveProducts;
using OjisanBackend.Application.Products.Queries.GetProductDetails;
using OjisanBackend.Domain.Constants;
using Microsoft.AspNetCore.Http.HttpResults;

namespace OjisanBackend.Web.Endpoints;

public class Products : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetActiveProducts).AllowAnonymous();
        groupBuilder.MapGet(GetProductDetails, "{id:guid}").AllowAnonymous();
        groupBuilder.MapPost(CreateProduct).RequireAuthorization();
    }

    public async Task<Ok<List<ProductBriefDto>>> GetActiveProducts(ISender sender)
    {
        var products = await sender.Send(new GetActiveProductsQuery());

        return TypedResults.Ok(products);
    }

    public async Task<Ok<ProductDto>> GetProductDetails(ISender sender, Guid id)
    {
        var product = await sender.Send(new GetProductDetailsQuery { ProductId = id });

        return TypedResults.Ok(product);
    }

    public async Task<Created<Guid>> CreateProduct(ISender sender, CreateProductCommand command)
    {
        var id = await sender.Send(command);

        return TypedResults.Created($"/{nameof(Products)}/{id}", id);
    }
}
