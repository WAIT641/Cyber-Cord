using Cyber_Cord.Api.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Cyber_Cord.Api.Middleware;

public class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        var statusCode = exception switch
        {
            NotFoundException => StatusCodes.Status404NotFound,
            ForbiddenException => StatusCodes.Status403Forbidden,
            BadRequestException => StatusCodes.Status400BadRequest,
            EmailSenderException => StatusCodes.Status502BadGateway,
            UnauthorizedException => StatusCodes.Status401Unauthorized,
            AlreadyExistsException => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status500InternalServerError
        };

        if (statusCode >= 500)
        {
            logger.LogError("GlobalExceptionHandler:TryHandleAsync StatusCode={} ErrorMessage={}", statusCode, exception.Message);
        }
        
        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Detail = exception.Message,
            Title = "An error occurred",
            Type = exception.GetType().Name
        };

        httpContext.Response.StatusCode = problemDetails.Status.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            Exception = exception,
            HttpContext = httpContext,
            ProblemDetails = problemDetails
        });
    }
}