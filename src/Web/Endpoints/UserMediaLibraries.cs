using Microsoft.AspNetCore.Http.HttpResults;
using OjisanBackend.Application.MediaLibraries.Common;
using OjisanBackend.Application.MediaLibraries.Queries.GetProductMediaLibraries;

namespace OjisanBackend.Web.Endpoints;

public class UserMediaLibraries : EndpointGroupBase
{
    public override string? GroupName => "user/products";

    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetMediaLibrariesForProduct, "{productId:guid}/media-libraries")
            .AllowAnonymous();
    }

    public async Task<Ok<IReadOnlyCollection<MediaLibraryDto>>> GetMediaLibrariesForProduct(
        ISender sender,
        Guid productId,
        CancellationToken cancellationToken)
    {
        var query = new GetProductMediaLibrariesQuery
        {
            ProductPublicId = productId
        };

        var result = await sender.Send(query, cancellationToken);

        return TypedResults.Ok(result);
    }
}

