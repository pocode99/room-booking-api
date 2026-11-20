using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using RoomBooking.Application.Exceptions;

namespace RoomBooking.Api.Middleware;

public class GlobalExceptionHandler : IExceptionHandler
{
    private readonly ILogger<GlobalExceptionHandler> _logger;

    public GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger)
    {
        _logger = logger;
    }

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext, Exception exception, CancellationToken ct)
    {
        var (status, title) = exception switch
        {
            BusinessRuleException => (StatusCodes.Status400BadRequest, "Invalid request"),
            InvalidCredentialsException => (StatusCodes.Status401Unauthorized, "Unauthorized"),
            ForbiddenException => (StatusCodes.Status403Forbidden, "Forbidden"),
            NotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict"),
            // Database constraint failures, e.g. a unique index or a foreign key.
            DbUpdateException => (StatusCodes.Status409Conflict, "Conflict"),
            _ => (StatusCodes.Status500InternalServerError, "An unexpected error occurred")
        };

        if (status == StatusCodes.Status500InternalServerError)
            _logger.LogError(exception, "Unhandled exception for {Path}", httpContext.Request.Path);
        else
            _logger.LogWarning("Request failed with {Status}: {Message}", status, exception.Message);

        // Never expose internal details of unexpected errors to the client.
        var detail = exception switch
        {
            BusinessRuleException
                or InvalidCredentialsException
                or ForbiddenException
                or NotFoundException
                or ConflictException => exception.Message,
            DbUpdateException => "The change conflicts with existing data.",
            _ => "An unexpected error occurred. Please try again later."
        };

        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Detail = detail,
            Instance = httpContext.Request.Path
        };

        httpContext.Response.StatusCode = status;
        await httpContext.Response.WriteAsJsonAsync(
            problem, options: null, contentType: "application/problem+json", cancellationToken: ct);

        return true;   // the exception is handled, stop the pipeline
    }
}