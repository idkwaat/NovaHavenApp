using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using NovaHaven.Api.Controllers;
using NovaHaven.Application.Features.Audit.Services;
using NovaHaven.Application.Features.Auth.Services;
using NovaHaven.Application.Features.Operations.Services;
using NovaHaven.Infrastructure.Data;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class PlatformControllerArchitectureTests
{
    [Fact]
    public void Authentication_controller_preserves_the_existing_route_surface()
    {
        AssertRoutes(typeof(AuthController), "api/v1/auth",
        [
            "GET /api/v1/auth/csrf",
            "GET /api/v1/auth/me",
            "POST /api/v1/auth/login",
            "POST /api/v1/auth/logout",
            "POST /api/v1/auth/register"
        ]);
    }

    [Fact]
    public void Admin_controllers_keep_their_existing_routes_and_policy()
    {
        AssertRoutes(typeof(AuditController), "api/v1/admin/audit", ["GET /api/v1/admin/audit"]);
        AssertRoutes(typeof(OperationsController), "api/v1/admin/operations", [
            "GET /api/v1/admin/operations/diagnostics"
        ]);

        AssertAdminOnly(typeof(AuditController));
        AssertAdminOnly(typeof(OperationsController));
    }

    [Fact]
    public void News_controllers_preserve_public_and_admin_routes_and_layer_boundaries()
    {
        var assembly = typeof(Program).Assembly;
        var publicController = assembly.GetType("NovaHaven.Api.Controllers.NewsController");
        var adminController = assembly.GetType("NovaHaven.Api.Controllers.AdminNewsController");
        var applicationService = typeof(UserAccountService).Assembly
            .GetType("NovaHaven.Application.Features.News.Services.NewsService");

        Assert.NotNull(publicController);
        Assert.NotNull(adminController);
        Assert.NotNull(applicationService);
        AssertRoutes(publicController, "api/v1/news", [
            "GET /api/v1/news",
            "GET /api/v1/news/{slug}"
        ]);
        AssertRoutes(adminController, "api/v1/admin/news", [
            "GET /api/v1/admin/news",
            "POST /api/v1/admin/news",
            "GET /api/v1/admin/news/{id:guid}",
            "PATCH /api/v1/admin/news/{id:guid}",
            "POST /api/v1/admin/news/{id:guid}/publish",
            "POST /api/v1/admin/news/{id:guid}/unpublish"
        ]);

        AssertAdminOnly(adminController);
        AssertMutationActionsRequireAntiforgery(adminController);
        AssertControllerDependsOnlyOnApplicationService(publicController, applicationService);
        AssertControllerDependsOnlyOnApplicationService(adminController, applicationService);
        Assert.Null(assembly.GetType("NovaHaven.Api.Endpoints.NewsEndpoints"));
    }

    [Fact]
    public void Catalog_controllers_preserve_public_and_admin_routes_and_layer_boundaries()
    {
        var apiAssembly = typeof(Program).Assembly;
        var applicationAssembly = typeof(UserAccountService).Assembly;
        var publicItems = apiAssembly.GetType("NovaHaven.Api.Controllers.CatalogItemsController");
        var adminItems = apiAssembly.GetType("NovaHaven.Api.Controllers.AdminCatalogItemsController");
        var publicRecipes = apiAssembly.GetType("NovaHaven.Api.Controllers.CatalogRecipesController");
        var adminRecipes = apiAssembly.GetType("NovaHaven.Api.Controllers.AdminCatalogRecipesController");
        var itemService = applicationAssembly.GetType("NovaHaven.Application.Features.Catalog.Services.CatalogItemService");
        var recipeService = applicationAssembly.GetType("NovaHaven.Application.Features.Catalog.Services.CatalogRecipeService");

        Assert.NotNull(publicItems);
        Assert.NotNull(adminItems);
        Assert.NotNull(publicRecipes);
        Assert.NotNull(adminRecipes);
        Assert.NotNull(itemService);
        Assert.NotNull(recipeService);

        AssertRoutes(publicItems, "api/v1/catalog/items", [
            "GET /api/v1/catalog/items",
            "GET /api/v1/catalog/items/{slug}"
        ]);
        AssertRoutes(adminItems, "api/v1/admin/catalog/items", [
            "GET /api/v1/admin/catalog/items",
            "POST /api/v1/admin/catalog/items",
            "GET /api/v1/admin/catalog/items/{id:guid}",
            "PATCH /api/v1/admin/catalog/items/{id:guid}",
            "POST /api/v1/admin/catalog/items/{id:guid}/publish",
            "POST /api/v1/admin/catalog/items/{id:guid}/unpublish"
        ]);
        AssertRoutes(publicRecipes, "api/v1/catalog/recipes", [
            "GET /api/v1/catalog/recipes",
            "GET /api/v1/catalog/recipes/{slug}"
        ]);
        AssertRoutes(adminRecipes, "api/v1/admin/catalog/recipes", [
            "GET /api/v1/admin/catalog/recipes",
            "POST /api/v1/admin/catalog/recipes",
            "GET /api/v1/admin/catalog/recipes/{id:guid}",
            "PATCH /api/v1/admin/catalog/recipes/{id:guid}",
            "POST /api/v1/admin/catalog/recipes/{id:guid}/publish",
            "POST /api/v1/admin/catalog/recipes/{id:guid}/unpublish"
        ]);

        AssertAdminOnly(adminItems);
        AssertAdminOnly(adminRecipes);
        AssertMutationActionsRequireAntiforgery(adminItems);
        AssertMutationActionsRequireAntiforgery(adminRecipes);
        AssertControllerDependsOnlyOnApplicationService(publicItems, itemService);
        AssertControllerDependsOnlyOnApplicationService(adminItems, itemService);
        AssertControllerDependsOnlyOnApplicationService(publicRecipes, recipeService);
        AssertControllerDependsOnlyOnApplicationService(adminRecipes, recipeService);
        Assert.DoesNotContain(typeof(NovaHaven.Api.Contracts.Catalog.CatalogRecipeRequest)
                .GetProperties()
                .SelectMany(property => property.PropertyType.GenericTypeArguments),
            type => type.Namespace?.StartsWith("NovaHaven.Application", StringComparison.Ordinal) == true);
        Assert.Null(apiAssembly.GetType("NovaHaven.Api.Endpoints.CatalogEndpoints"));
        Assert.Null(apiAssembly.GetType("NovaHaven.Api.Endpoints.CatalogRecipeEndpoints"));
    }

    [Fact]
    public void Reward_controllers_preserve_public_and_admin_routes_and_layer_boundaries()
    {
        var apiAssembly = typeof(Program).Assembly;
        var applicationAssembly = typeof(UserAccountService).Assembly;
        var publicController = apiAssembly.GetType("NovaHaven.Api.Controllers.RewardsController");
        var adminController = apiAssembly.GetType("NovaHaven.Api.Controllers.AdminRewardsController");
        var rewardService = applicationAssembly.GetType("NovaHaven.Application.Features.Rewards.Services.RewardService");

        Assert.NotNull(publicController);
        Assert.NotNull(adminController);
        Assert.NotNull(rewardService);
        AssertRoutes(publicController, "api/v1/rewards", [
            "GET /api/v1/rewards",
            "GET /api/v1/rewards/{slug}"
        ]);
        AssertRoutes(adminController, "api/v1/admin/rewards", [
            "GET /api/v1/admin/rewards",
            "POST /api/v1/admin/rewards",
            "GET /api/v1/admin/rewards/{id:guid}",
            "PATCH /api/v1/admin/rewards/{id:guid}",
            "POST /api/v1/admin/rewards/{id:guid}/publish",
            "POST /api/v1/admin/rewards/{id:guid}/unpublish"
        ]);

        AssertAdminOnly(adminController);
        AssertMutationActionsRequireAntiforgery(adminController);
        AssertControllerDependsOnlyOnApplicationService(publicController, rewardService);
        AssertControllerDependsOnlyOnApplicationService(adminController, rewardService);
        Assert.Null(apiAssembly.GetType("NovaHaven.Api.Endpoints.RewardEndpoints"));
    }

    [Fact]
    public void Knowledge_controllers_preserve_public_and_admin_routes_and_layer_boundaries()
    {
        var apiAssembly = typeof(Program).Assembly;
        var applicationAssembly = typeof(UserAccountService).Assembly;
        var publicController = apiAssembly.GetType("NovaHaven.Api.Controllers.KnowledgeController");
        var adminController = apiAssembly.GetType("NovaHaven.Api.Controllers.AdminKnowledgeController");
        var knowledgeService = applicationAssembly.GetType("NovaHaven.Application.Features.Knowledge.Services.KnowledgeService");

        Assert.NotNull(publicController);
        Assert.NotNull(adminController);
        Assert.NotNull(knowledgeService);
        AssertRoutes(publicController, "api/v1/knowledge", [
            "GET /api/v1/knowledge",
            "GET /api/v1/knowledge/{kind}",
            "GET /api/v1/knowledge/{kind}/{slug}"
        ]);
        AssertRoutes(adminController, "api/v1/admin/knowledge", [
            "GET /api/v1/admin/knowledge/{kind}",
            "POST /api/v1/admin/knowledge/{kind}",
            "GET /api/v1/admin/knowledge/{kind}/{id:guid}",
            "PATCH /api/v1/admin/knowledge/{kind}/{id:guid}",
            "POST /api/v1/admin/knowledge/{kind}/{id:guid}/publish",
            "POST /api/v1/admin/knowledge/{kind}/{id:guid}/unpublish"
        ]);

        AssertAdminOnly(adminController);
        AssertMutationActionsRequireAntiforgery(adminController);
        AssertControllerDependsOnlyOnApplicationService(publicController, knowledgeService);
        AssertControllerDependsOnlyOnApplicationService(adminController, knowledgeService);
        Assert.Null(apiAssembly.GetType("NovaHaven.Api.Endpoints.KnowledgeEndpoints"));
    }

    [Fact]
    public void Community_controllers_preserve_routes_and_layer_boundaries()
    {
        var apiAssembly = typeof(Program).Assembly;
        var applicationAssembly = typeof(UserAccountService).Assembly;
        var publicController = apiAssembly.GetType("NovaHaven.Api.Controllers.CommunityController");
        var adminController = apiAssembly.GetType("NovaHaven.Api.Controllers.AdminCommunityController");
        var communityService = applicationAssembly.GetType("NovaHaven.Application.Features.Community.Services.CommunityService");

        Assert.NotNull(publicController);
        Assert.NotNull(adminController);
        Assert.NotNull(communityService);
        AssertRoutes(publicController, "api/v1/community", [
            "GET /api/v1/community",
            "GET /api/v1/community/{kind}",
            "GET /api/v1/community/{kind}/{slug}"
        ]);
        AssertRoutes(adminController, "api/v1/admin/community", [
            "GET /api/v1/admin/community/{kind}",
            "POST /api/v1/admin/community/{kind}",
            "GET /api/v1/admin/community/{kind}/{id:guid}",
            "PATCH /api/v1/admin/community/{kind}/{id:guid}",
            "POST /api/v1/admin/community/{kind}/{id:guid}/publish",
            "POST /api/v1/admin/community/{kind}/{id:guid}/unpublish",
            "GET /api/v1/admin/community/event/{id:guid}/registrations",
            "POST /api/v1/admin/community/event/{id:guid}/registrations",
            "PATCH /api/v1/admin/community/event/{id:guid}/registrations/{registrationId:guid}"
        ]);

        AssertAdminOnly(adminController);
        AssertMutationActionsRequireAntiforgery(adminController);
        AssertControllerDependsOnlyOnApplicationService(publicController, communityService);
        AssertControllerDependsOnlyOnApplicationService(adminController, communityService);
        Assert.Null(apiAssembly.GetType("NovaHaven.Api.Endpoints.CommunityEndpoints"));
    }

    [Fact]
    public void Integration_controllers_preserve_public_status_and_admin_management_routes()
    {
        var apiAssembly = typeof(Program).Assembly;
        var applicationAssembly = typeof(UserAccountService).Assembly;
        var publicController = apiAssembly.GetType("NovaHaven.Api.Controllers.IntegrationsController");
        var adminController = apiAssembly.GetType("NovaHaven.Api.Controllers.AdminIntegrationsController");
        var integrationService = applicationAssembly.GetType("NovaHaven.Application.Features.Integration.Services.IntegrationCapabilityService");

        Assert.NotNull(publicController);
        Assert.NotNull(adminController);
        Assert.NotNull(integrationService);
        AssertRoutes(publicController, "api/v1/integrations/status", ["GET /api/v1/integrations/status"]);
        AssertRoutes(adminController, "api/v1/admin/integrations", [
            "GET /api/v1/admin/integrations/capabilities",
            "PATCH /api/v1/admin/integrations/capabilities/{key}"
        ]);

        AssertAdminOnly(adminController);
        AssertMutationActionsRequireAntiforgery(adminController);
        AssertControllerDependsOnlyOnApplicationService(publicController, integrationService);
        AssertControllerDependsOnlyOnApplicationService(adminController, integrationService);
        Assert.Null(apiAssembly.GetType("NovaHaven.Api.Endpoints.IntegrationEndpoints"));
    }

    [Fact]
    public void Commerce_controllers_preserve_offer_and_order_routes_and_layer_boundaries()
    {
        var apiAssembly = typeof(Program).Assembly;
        var applicationAssembly = typeof(UserAccountService).Assembly;
        var offers = apiAssembly.GetType("NovaHaven.Api.Controllers.CommerceOffersController");
        var adminOffers = apiAssembly.GetType("NovaHaven.Api.Controllers.AdminCommerceOffersController");
        var orders = apiAssembly.GetType("NovaHaven.Api.Controllers.CommerceOrdersController");
        var adminOrders = apiAssembly.GetType("NovaHaven.Api.Controllers.AdminCommerceOrdersController");
        var offerService = applicationAssembly.GetType("NovaHaven.Application.Features.Commerce.Services.CommerceOfferService");
        var orderService = applicationAssembly.GetType("NovaHaven.Application.Features.Commerce.Services.CommerceOrderService");

        Assert.NotNull(offers);
        Assert.NotNull(adminOffers);
        Assert.NotNull(orders);
        Assert.NotNull(adminOrders);
        Assert.NotNull(offerService);
        Assert.NotNull(orderService);
        AssertRoutes(offers, "api/v1/commerce/offers", [
            "GET /api/v1/commerce/offers",
            "GET /api/v1/commerce/offers/{slug}"
        ]);
        AssertRoutes(adminOffers, "api/v1/admin/commerce/offers", [
            "GET /api/v1/admin/commerce/offers",
            "POST /api/v1/admin/commerce/offers",
            "GET /api/v1/admin/commerce/offers/{id:guid}",
            "PATCH /api/v1/admin/commerce/offers/{id:guid}",
            "POST /api/v1/admin/commerce/offers/{id:guid}/publish",
            "POST /api/v1/admin/commerce/offers/{id:guid}/unpublish"
        ]);
        AssertRoutes(orders, "api/v1/commerce/orders", ["POST /api/v1/commerce/orders"]);
        AssertRoutes(adminOrders, "api/v1/admin/commerce/orders", ["GET /api/v1/admin/commerce/orders"]);
        AssertAdminOnly(adminOffers);
        AssertAdminOnly(adminOrders);
        AssertMutationActionsRequireAntiforgery(adminOffers);
        AssertControllerDependsOnlyOnApplicationService(offers, offerService);
        AssertControllerDependsOnlyOnApplicationService(adminOffers, offerService);
        AssertControllerDependsOnlyOnApplicationService(orders, orderService);
        AssertControllerDependsOnlyOnApplicationService(adminOrders, orderService);
        Assert.Null(apiAssembly.GetType("NovaHaven.Api.Endpoints.CommerceEndpoints"));
        Assert.Null(apiAssembly.GetType("NovaHaven.Api.Endpoints.CommerceOrderEndpoints"));
    }

    [Fact]
    public void Notification_controllers_preserve_player_and_admin_routes_and_layer_boundaries()
    {
        var apiAssembly = typeof(Program).Assembly;
        var applicationAssembly = typeof(UserAccountService).Assembly;
        var playerController = apiAssembly.GetType("NovaHaven.Api.Controllers.NotificationsController");
        var adminController = apiAssembly.GetType("NovaHaven.Api.Controllers.AdminNotificationsController");
        var notificationService = applicationAssembly.GetType("NovaHaven.Application.Features.Notifications.Services.UserNotificationService");

        Assert.NotNull(playerController);
        Assert.NotNull(adminController);
        Assert.NotNull(notificationService);
        AssertRoutes(playerController, "api/v1/notifications", [
            "GET /api/v1/notifications",
            "GET /api/v1/notifications/unread-count",
            "PUT /api/v1/notifications/{id:guid}/read",
            "POST /api/v1/notifications/read-all",
            "GET /api/v1/notifications/push/config",
            "PUT /api/v1/notifications/push/subscriptions",
            "DELETE /api/v1/notifications/push/subscriptions"
        ]);
        AssertRoutes(adminController, "api/v1/admin/notifications", ["POST /api/v1/admin/notifications"]);
        AssertAdminOnly(adminController);
        AssertMutationActionsRequireAntiforgery(playerController);
        AssertMutationActionsRequireAntiforgery(adminController);
        AssertControllerDependsOnlyOnApplicationService(playerController, notificationService);
        AssertControllerDependsOnlyOnApplicationService(adminController, notificationService);
        Assert.Null(apiAssembly.GetType("NovaHaven.Api.Endpoints.NotificationEndpoints"));
    }

    [Theory]
    [InlineData(typeof(AuthController), typeof(UserAccountService))]
    [InlineData(typeof(AuditController), typeof(AuditService))]
    [InlineData(typeof(OperationsController), typeof(OperationsService))]
    public void Controllers_depend_on_application_services_not_persistence_or_identity(
        Type controllerType,
        Type applicationServiceType)
    {
        var dependencies = Assert.Single(controllerType.GetConstructors())
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        Assert.Contains(applicationServiceType, dependencies);
        Assert.DoesNotContain(typeof(NovaDbContext), dependencies);
        Assert.DoesNotContain(dependencies, dependency =>
            dependency.Namespace?.StartsWith("Microsoft.AspNetCore.Identity", StringComparison.Ordinal) == true);
    }

    [Fact]
    public void Api_contracts_do_not_depend_on_application_commands_or_results()
    {
        var apiAssembly = typeof(Program).Assembly;
        var leaks = apiAssembly.GetTypes()
            .Where(type => type.Namespace?.StartsWith("NovaHaven.Api.Contracts.", StringComparison.Ordinal) == true)
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public))
            .SelectMany(property => ExpandPropertyType(property.PropertyType))
            .Where(type => type.Namespace?.StartsWith("NovaHaven.Application", StringComparison.Ordinal) == true)
            .ToArray();

        Assert.Empty(leaks);
    }

    private static void AssertAdminOnly(Type controllerType)
    {
        var authorization = controllerType.GetCustomAttributes<AuthorizeAttribute>(inherit: true);
        Assert.Contains(authorization, attribute => attribute.Policy == "AdminOnly");
    }

    private static void AssertMutationActionsRequireAntiforgery(Type controllerType)
    {
        var mutationMethods = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .Where(method => method.GetCustomAttributes<HttpMethodAttribute>()
                .Any(attribute => attribute.HttpMethods.Any(methodName => methodName != "GET")));

        Assert.NotEmpty(mutationMethods);
        Assert.All(mutationMethods, method =>
            Assert.NotNull(method.GetCustomAttribute<ValidateAntiForgeryTokenAttribute>()));
    }

    private static void AssertControllerDependsOnlyOnApplicationService(Type controllerType, Type applicationServiceType)
    {
        var dependencies = Assert.Single(controllerType.GetConstructors())
            .GetParameters()
            .Select(parameter => parameter.ParameterType)
            .ToArray();

        Assert.Equal(new[] { applicationServiceType }, dependencies);
        Assert.DoesNotContain(typeof(NovaDbContext), dependencies);
    }

    private static void AssertRoutes(Type controllerType, string baseRoute, string[] expectedRoutes)
    {
        Assert.NotNull(controllerType.GetCustomAttribute<ApiControllerAttribute>());
        Assert.Equal(baseRoute, controllerType.GetCustomAttribute<RouteAttribute>()?.Template);

        var routes = controllerType.GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly)
            .SelectMany(method => method.GetCustomAttributes<HttpMethodAttribute>()
                .Select(attribute =>
                {
                    var route = string.IsNullOrWhiteSpace(attribute.Template)
                        ? baseRoute
                        : $"{baseRoute}/{attribute.Template.TrimStart('/')}";
                    return $"{attribute.HttpMethods.Single()} /{route.TrimStart('/').TrimEnd('/')}";
                }))
            .OrderBy(route => route, StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expectedRoutes.OrderBy(route => route, StringComparer.Ordinal), routes);
    }

    private static IEnumerable<Type> ExpandPropertyType(Type type)
    {
        yield return type;
        if (type.HasElementType && type.GetElementType() is Type elementType)
        {
            foreach (var nested in ExpandPropertyType(elementType)) yield return nested;
        }
        if (!type.IsGenericType) yield break;
        foreach (var argument in type.GetGenericArguments())
        {
            foreach (var nested in ExpandPropertyType(argument)) yield return nested;
        }
    }
}
