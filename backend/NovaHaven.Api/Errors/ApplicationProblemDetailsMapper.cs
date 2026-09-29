using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Application.Common.Results;

namespace NovaHaven.Api.Errors;

public static class ApplicationProblemDetailsMapper
{
    public static ObjectResult ToActionResult(ApplicationError error)
    {
        ArgumentNullException.ThrowIfNull(error);

        var statusCode = ResolveStatusCode(error);
        ProblemDetails problem = statusCode == StatusCodes.Status400BadRequest
            && error.ValidationErrors is { Count: > 0 }
                ? new ValidationProblemDetails(error.ValidationErrors.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value,
                    StringComparer.Ordinal))
                : new ProblemDetails();

        problem.Status = statusCode;
        problem.Title = error.Message;
        problem.Extensions["code"] = error.Code;

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" }
        };
    }

    private static int ResolveStatusCode(ApplicationError error)
    {
        if (error.ValidationErrors is { Count: > 0 }) return StatusCodes.Status400BadRequest;

        return error.Code switch
        {
            "validation.failed" => StatusCodes.Status400BadRequest,
            "wiki.media.invalid" => StatusCodes.Status400BadRequest,
            "http.precondition-required" => StatusCodes.Status428PreconditionRequired,
            "http.precondition-failed" => StatusCodes.Status412PreconditionFailed,
            _ when error.Code.EndsWith(".not-found", StringComparison.Ordinal) => StatusCodes.Status404NotFound,
            _ when error.Code.EndsWith(".conflict", StringComparison.Ordinal) => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError
        };
    }

}
