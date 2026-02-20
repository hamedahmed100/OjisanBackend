using OjisanBackend.Application.Uploads.Commands.UploadBadgeImage;
using Microsoft.AspNetCore.Http.HttpResults;

namespace OjisanBackend.Web.Endpoints;

public class Uploads : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(UploadBadgeImage, "badge")
            .RequireAuthorization()
            .DisableAntiforgery(); // Required for file uploads in Minimal APIs
    }

    public async Task<Results<Ok<string>, BadRequest>> UploadBadgeImage(
        ISender sender,
        IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return TypedResults.BadRequest();
        }

        // Open the stream and create the command
        // The stream will be disposed automatically when the using block exits
        await using var stream = file.OpenReadStream();
        
        var command = new UploadBadgeImageCommand
        {
            Content = stream,
            FileName = file.FileName ?? string.Empty,
            ContentType = file.ContentType ?? string.Empty
        };

        var imageUrl = await sender.Send(command);

        return TypedResults.Ok(imageUrl);
    }
}
