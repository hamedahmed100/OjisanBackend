using OjisanBackend.Application.Payments.Commands.CreatePaymentSession;
using OjisanBackend.Application.Payments.Commands.ProcessWebhook;
using Microsoft.AspNetCore.Http.HttpResults;

namespace OjisanBackend.Web.Endpoints;

public class Payments : EndpointGroupBase
{
    public override void Map(RouteGroupBuilder groupBuilder)
    {
        groupBuilder.MapPost(CreatePaymentSession, "session").RequireAuthorization();
        groupBuilder.MapPost(ProcessWebhook, "webhook").AllowAnonymous().RequireRateLimiting("AnonymousPolicy");
    }

    public async Task<Ok<string>> CreatePaymentSession(ISender sender, CreatePaymentSessionCommand command)
    {
        var checkoutUrl = await sender.Send(command);

        return TypedResults.Ok(checkoutUrl);
    }

    public async Task<Results<Ok, BadRequest, UnauthorizedHttpResult>> ProcessWebhook(
        ISender sender,
        HttpContext httpContext)
    {
        // Read the raw request body as string for signature validation
        httpContext.Request.EnableBuffering();
        using var reader = new StreamReader(httpContext.Request.Body, leaveOpen: true);
        var payload = await reader.ReadToEndAsync();
        httpContext.Request.Body.Position = 0; // Reset stream position

        // Extract signature from header (Fatorah typically uses X-Fatorah-Signature or similar)
        if (!httpContext.Request.Headers.TryGetValue("X-Fatorah-Signature", out var signatureHeader))
        {
            // Try alternative header names
            if (!httpContext.Request.Headers.TryGetValue("X-Signature", out signatureHeader))
            {
                return TypedResults.BadRequest();
            }
        }

        var signature = signatureHeader.ToString();

        if (string.IsNullOrWhiteSpace(signature))
        {
            return TypedResults.BadRequest();
        }

        try
        {
            var command = new ProcessPaymentWebhookCommand
            {
                Payload = payload,
                Signature = signature
            };

            await sender.Send(command);

            return TypedResults.Ok();
        }
        catch (UnauthorizedAccessException)
        {
            return TypedResults.Unauthorized();
        }
    }
}
