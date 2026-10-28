using System.Text.Json;
using FluentValidation;
using IndustrialOps.Application.Common.Exceptions;
using IndustrialOps.Domain.Common;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace IndustrialOps.Api.ErrorHandling;

/// <summary>
/// Single place that turns any exception thrown while handling a request into a
/// standard RFC 9457 "Problem Details" JSON response. Controllers and services never
/// write try/catch for HTTP concerns: they just throw meaningful exceptions.
/// </summary>
internal sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problem = exception switch
        {
            ValidationException validation => new HttpValidationProblemDetails(ToErrorDictionary(validation))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "One or more validation errors occurred.",
            },
            NotFoundException => Problem(StatusCodes.Status404NotFound, "Resource not found.", exception.Message),
            ConflictException => Problem(StatusCodes.Status409Conflict, "Conflict with the current state of the resource.", exception.Message),
            DomainException => Problem(StatusCodes.Status422UnprocessableEntity, "Business rule violation.", exception.Message),
            BadHttpRequestException badRequest => Problem(badRequest.StatusCode, "Bad request.", badRequest.Message),
            // Unknown errors: never expose internal details (stack trace, SQL, etc.) to the client.
            _ => Problem(StatusCodes.Status500InternalServerError, "An unexpected error occurred.", null),
        };

        if (problem.Status >= StatusCodes.Status500InternalServerError)
            logger.LogError(exception, "Unhandled exception while processing {Method} {Path}",
                httpContext.Request.Method, httpContext.Request.Path);
        else
            logger.LogInformation("Request {Method} {Path} failed with {StatusCode}: {Message}",
                httpContext.Request.Method, httpContext.Request.Path, problem.Status, exception.Message);

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception,
        });
    }

    private static ProblemDetails Problem(int status, string title, string? detail) =>
        new() { Status = status, Title = title, Detail = detail };

    // { "plannedQuantity": ["'Planned Quantity' must be greater than '0'."] }
    private static Dictionary<string, string[]> ToErrorDictionary(ValidationException exception) =>
        exception.Errors
            .GroupBy(e => JsonNamingPolicy.CamelCase.ConvertName(e.PropertyName))
            .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).Distinct().ToArray());
}
