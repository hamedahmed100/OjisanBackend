using OjisanBackend.Application.Common.Exceptions;
using OjisanBackend.Domain.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace OjisanBackend.Web.Infrastructure;

public class CustomExceptionHandler : IExceptionHandler
{
    private readonly Dictionary<Type, Func<HttpContext, Exception, Task>> _exceptionHandlers;

    public CustomExceptionHandler()
    {
        // Register known exception types and handlers.
        _exceptionHandlers = new()
            {
                { typeof(ValidationException), HandleValidationException },
                { typeof(OjisanBackend.Application.Common.Exceptions.NotFoundException), HandleNotFoundException },
                { typeof(UnauthorizedAccessException), HandleUnauthorizedAccessException },
                { typeof(ForbiddenAccessException), HandleForbiddenAccessException },
                { typeof(GroupNotAcceptingMembersException), HandleDomainException },
                { typeof(GroupFullException), HandleDomainException },
                { typeof(MemberAlreadyInGroupException), HandleDomainException },
                { typeof(UserNotMemberOfGroupException), HandleDomainException },
                { typeof(DuplicateSubmissionException), HandleDomainException },
                { typeof(SubmissionNotRejectedException), HandleDomainException },
            };
    }

    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var exceptionType = exception.GetType();

        if (_exceptionHandlers.ContainsKey(exceptionType))
        {
            await _exceptionHandlers[exceptionType].Invoke(httpContext, exception);
            return true;
        }

        return false;
    }

    private async Task HandleValidationException(HttpContext httpContext, Exception ex)
    {
        var exception = (ValidationException)ex;

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;

        await httpContext.Response.WriteAsJsonAsync(new ValidationProblemDetails(exception.Errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        });
    }

    private async Task HandleNotFoundException(HttpContext httpContext, Exception ex)
    {
        var exception = (OjisanBackend.Application.Common.Exceptions.NotFoundException)ex;

        httpContext.Response.StatusCode = StatusCodes.Status404NotFound;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails()
        {
            Status = StatusCodes.Status404NotFound,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.4",
            Title = "The specified resource was not found.",
            Detail = exception.Message
        });
    }

    private async Task HandleUnauthorizedAccessException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status401Unauthorized;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status401Unauthorized,
            Title = "Unauthorized",
            Type = "https://tools.ietf.org/html/rfc7235#section-3.1"
        });
    }

    private async Task HandleForbiddenAccessException(HttpContext httpContext, Exception ex)
    {
        httpContext.Response.StatusCode = StatusCodes.Status403Forbidden;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = StatusCodes.Status403Forbidden,
            Title = "Forbidden",
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.3"
        });
    }

    private async Task HandleDomainException(HttpContext httpContext, Exception ex)
    {
        // Map domain exceptions to appropriate HTTP status codes
        var statusCode = ex switch
        {
            GroupNotAcceptingMembersException => StatusCodes.Status400BadRequest,
            GroupFullException => StatusCodes.Status400BadRequest,
            MemberAlreadyInGroupException => StatusCodes.Status409Conflict,
            DuplicateSubmissionException => StatusCodes.Status409Conflict,
            SubmissionNotRejectedException => StatusCodes.Status400BadRequest,
            UserNotMemberOfGroupException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };

        httpContext.Response.StatusCode = statusCode;

        await httpContext.Response.WriteAsJsonAsync(new ProblemDetails
        {
            Status = statusCode,
            Title = ex.GetType().Name,
            Detail = ex.Message,
            Type = "https://tools.ietf.org/html/rfc7231#section-6.5.1"
        });
    }
}
