using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using NovaHaven.Api.Controllers;
using NovaHaven.Application.Features.Wiki.Services;
using NovaHaven.Infrastructure.Data;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class AdminWikiArticlesControllerArchitectureTests
{
    [Fact]
    public void Article_editorial_routes_are_admin_only_and_csrf_protected()
    {
        var controller = typeof(AdminWikiArticlesController);

        Assert.NotNull(controller.GetCustomAttribute<ApiControllerAttribute>());
        Assert.Equal("api/v1/admin/wiki", controller.GetCustomAttribute<RouteAttribute>()?.Template);
        Assert.Equal("AdminOnly", controller.GetCustomAttribute<AuthorizeAttribute>()?.Policy);

        var methods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        var routes = methods
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>()
                .Select(attribute => $"{attribute.HttpMethods.Single()} /api/v1/admin/wiki/{attribute.Template}"))
            .OrderBy(route => route, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(
            [
                "GET /api/v1/admin/wiki/articles",
                "GET /api/v1/admin/wiki/articles/{id:guid}",
                "GET /api/v1/admin/wiki/articles/{id:guid}/revisions",
                "PATCH /api/v1/admin/wiki/articles/{id:guid}",
                "POST /api/v1/admin/wiki/articles",
                "POST /api/v1/admin/wiki/articles/{id:guid}/publish",
                "POST /api/v1/admin/wiki/articles/{id:guid}/revisions/{revisionId:guid}/restore",
                "POST /api/v1/admin/wiki/articles/{id:guid}/unpublish"
            ],
            routes);

        foreach (var method in methods.Where(method => method.GetCustomAttributes<HttpMethodAttribute>()
                     .Any(attribute => attribute.HttpMethods.Any(httpMethod => httpMethod != "GET"))))
        {
            Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        }

        var constructor = Assert.Single(controller.GetConstructors());
        var dependencies = constructor.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        Assert.Contains(typeof(WikiArticleService), dependencies);
        Assert.DoesNotContain(typeof(NovaDbContext), dependencies);
    }
}
