using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using NovaHaven.Api.Errors;
using NovaHaven.Application.Common.Results;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class ApplicationProblemDetailsMapperTests
{
    [Fact]
    public void Validation_error_maps_to_problem_details_with_field_errors()
    {
        var error = new ApplicationError(
            "validation.failed",
            "The supplied article data is invalid.",
            new Dictionary<string, string[]> { ["slug"] = ["Slug is required."] });

        var result = ApplicationProblemDetailsMapper.ToActionResult(error);

        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);
        var problem = Assert.IsType<ValidationProblemDetails>(result.Value);
        Assert.Equal(error.Message, problem.Title);
        Assert.Null(problem.Detail);
        Assert.Equal(error.Code, problem.Extensions["code"]);
        Assert.Equal("Slug is required.", Assert.Single(problem.Errors["slug"]));
    }

    [Theory]
    [InlineData("wiki.article.not-found", StatusCodes.Status404NotFound)]
    [InlineData("commerce.checkout.conflict", StatusCodes.Status409Conflict)]
    [InlineData("http.precondition-required", StatusCodes.Status428PreconditionRequired)]
    [InlineData("http.precondition-failed", StatusCodes.Status412PreconditionFailed)]
    public void Known_application_error_codes_keep_their_http_status(string code, int expectedStatus)
    {
        var result = ApplicationProblemDetailsMapper.ToActionResult(new ApplicationError(code, "Safe detail."));

        Assert.Equal(expectedStatus, result.StatusCode);
        var problem = Assert.IsType<ProblemDetails>(result.Value);
        Assert.Equal(code, problem.Extensions["code"]);
    }
}
