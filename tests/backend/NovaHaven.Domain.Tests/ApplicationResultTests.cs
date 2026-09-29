using NovaHaven.Application.Common.Results;
using Xunit;

namespace NovaHaven.Domain.Tests;

public sealed class ApplicationResultTests
{
    [Fact]
    public void Success_contains_value_and_no_error()
    {
        var result = ApplicationResult<string>.Success("published");

        Assert.True(result.IsSuccess);
        Assert.Equal("published", result.Value);
        Assert.Null(result.Error);
    }

    [Fact]
    public void Failure_contains_safe_error_and_no_value()
    {
        var error = new ApplicationError(
            "wiki.article.not-found",
            "The requested article was not found.");

        var result = ApplicationResult<string>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Null(result.Value);
        Assert.Same(error, result.Error);
    }

    [Fact]
    public void Failure_preserves_field_validation_errors()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["slug"] = ["Slug is required."]
        };
        var error = new ApplicationError(
            "validation.failed",
            "One or more fields are invalid.",
            errors);

        var result = ApplicationResult<int>.Failure(error);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.failed", result.Error!.Code);
        Assert.Equal("Slug is required.", Assert.Single(result.Error.ValidationErrors!["slug"]));
    }
}
