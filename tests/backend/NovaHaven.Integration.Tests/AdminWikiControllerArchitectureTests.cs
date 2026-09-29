using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using NovaHaven.Api.Controllers;
using NovaHaven.Infrastructure.Data;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class AdminWikiControllerArchitectureTests
{
    [Fact]
    public void Tag_controller_keeps_admin_routes_authorization_and_csrf()
    {
        AssertController(
            typeof(AdminWikiTagsController),
            [
                "DELETE /api/v1/admin/wiki/tags/{id:guid}",
                "GET /api/v1/admin/wiki/tags",
                "PATCH /api/v1/admin/wiki/tags/{id:guid}",
                "POST /api/v1/admin/wiki/tags"
            ]);
    }

    [Fact]
    public void Category_controller_keeps_admin_routes_authorization_and_csrf()
    {
        AssertController(
            typeof(AdminWikiCategoriesController),
            [
                "DELETE /api/v1/admin/wiki/categories/{id:guid}",
                "GET /api/v1/admin/wiki/categories",
                "GET /api/v1/admin/wiki/categories/{id:guid}",
                "PATCH /api/v1/admin/wiki/categories/{id:guid}",
                "POST /api/v1/admin/wiki/categories"
            ]);
    }

    private static void AssertController(Type controller, string[] expectedRoutes)
    {
        Assert.NotNull(controller.GetCustomAttribute<ApiControllerAttribute>());
        Assert.Equal("api/v1/admin/wiki", controller.GetCustomAttribute<RouteAttribute>()?.Template);
        Assert.Equal("AdminOnly", controller.GetCustomAttribute<AuthorizeAttribute>()?.Policy);

        var methods = controller.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);
        var routes = methods
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>()
                .Select(attribute => $"{attribute.HttpMethods.Single()} /api/v1/admin/wiki/{attribute.Template}"))
            .OrderBy(route => route, StringComparer.Ordinal)
            .ToArray();
        Assert.Equal(expectedRoutes.OrderBy(route => route, StringComparer.Ordinal), routes);

        foreach (var method in methods.Where(method => method.GetCustomAttributes<HttpMethodAttribute>()
                     .Any(attribute => attribute.HttpMethods.Any(httpMethod => httpMethod != "GET"))))
        {
            Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>());
        }

        var constructor = Assert.Single(controller.GetConstructors());
        Assert.DoesNotContain(typeof(NovaDbContext),
            constructor.GetParameters().Select(parameter => parameter.ParameterType));
    }
}
