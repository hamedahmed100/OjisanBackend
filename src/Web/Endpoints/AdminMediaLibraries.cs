using Microsoft.AspNetCore.Http.HttpResults;
using OjisanBackend.Application.MediaLibraries.Commands.AddMediaLibraryImages;
using OjisanBackend.Application.MediaLibraries.Commands.CreateMediaLibrary;
using OjisanBackend.Application.MediaLibraries.Commands.DeleteMediaLibraryImage;
using OjisanBackend.Application.MediaLibraries.Common;
using OjisanBackend.Application.MediaLibraries.Queries.GetAllMediaLibraries;
using OjisanBackend.Application.MediaLibraries.Queries.GetMediaLibraryDetails;
using OjisanBackend.Domain.Constants;

namespace OjisanBackend.Web.Endpoints;

public class AdminMediaLibraries : EndpointGroupBase
{
    public override string? GroupName => "admin/media-libraries";

    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapGet(GetAllMediaLibraries, "")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapGet(GetMediaLibraryDetails, "{id:guid}")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));

        groupBuilder.MapPost(CreateMediaLibrary, "")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator))
            .DisableAntiforgery();

        groupBuilder.MapPut(AddImages, "{id:guid}/images")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator))
            .DisableAntiforgery();

        groupBuilder.MapDelete(DeleteImage, "{id:guid}/images/{imageId:guid}")
            .RequireAuthorization(policy => policy.RequireRole(Roles.Administrator));
    }

    public async Task<Ok<IReadOnlyCollection<AdminMediaLibraryDto>>> GetAllMediaLibraries(
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetAllMediaLibrariesQuery(), cancellationToken);
        return TypedResults.Ok(result);
    }

    public async Task<Ok<AdminMediaLibraryDto>> GetMediaLibraryDetails(
        ISender sender,
        Guid id,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetMediaLibraryDetailsQuery { LibraryPublicId = id }, cancellationToken);
        return TypedResults.Ok(result);
    }

    public async Task<Results<Created<CreateMediaLibraryResult>, BadRequest<string>>> CreateMediaLibrary(
        ISender sender,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return TypedResults.BadRequest("Form-data content type is required.");
        }

        var form = await request.ReadFormAsync(cancellationToken);

        var title = form["title"].ToString();
        var description = form["description"].ToString();
        var productIdsRaw = form["productIds"];

        var productPublicIds = new List<Guid>();
        foreach (var value in productIdsRaw)
        {
            if (Guid.TryParse(value, out var guid))
            {
                productPublicIds.Add(guid);
            }
        }

        var files = new List<FileUploadDto>();
        foreach (var file in form.Files)
        {
            files.Add(new FileUploadDto
            {
                Content = file.OpenReadStream(),
                FileName = file.FileName ?? string.Empty,
                ContentType = file.ContentType ?? string.Empty
            });
        }

        var command = new CreateMediaLibraryCommand
        {
            Title = title,
            Description = description,
            ProductPublicIds = productPublicIds,
            Files = files
        };

        var result = await sender.Send(command, cancellationToken);

        return TypedResults.Created($"/api/admin/media-libraries/{result.PublicId}", result);
    }

    public async Task<Results<Ok<IReadOnlyCollection<MediaLibraryImageDto>>, BadRequest<string>>> AddImages(
        ISender sender,
        Guid id,
        HttpRequest request,
        CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType)
        {
            return TypedResults.BadRequest("Form-data content type is required.");
        }

        var form = await request.ReadFormAsync(cancellationToken);

        var files = new List<FileUploadDto>();
        foreach (var file in form.Files)
        {
            files.Add(new FileUploadDto
            {
                Content = file.OpenReadStream(),
                FileName = file.FileName ?? string.Empty,
                ContentType = file.ContentType ?? string.Empty
            });
        }

        var command = new AddMediaLibraryImagesCommand
        {
            LibraryPublicId = id,
            Files = files
        };

        var images = await sender.Send(command, cancellationToken);

        return TypedResults.Ok(images);
    }

    public async Task<Results<NoContent, BadRequest>> DeleteImage(
        ISender sender,
        Guid id,
        Guid imageId,
        CancellationToken cancellationToken)
    {
        var command = new DeleteMediaLibraryImageCommand
        {
            LibraryPublicId = id,
            ImagePublicId = imageId
        };

        await sender.Send(command, cancellationToken);

        return TypedResults.NoContent();
    }
}

