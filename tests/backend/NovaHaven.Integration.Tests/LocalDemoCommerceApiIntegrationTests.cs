using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaHaven.Domain.Commerce;
using NovaHaven.Domain.Commerce.Entities;
using NovaHaven.Infrastructure.Data;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class LocalDemoCommerceApiIntegrationTests : IClassFixture<LocalApiFactory>
{
    private readonly LocalApiFactory factory;

    public LocalDemoCommerceApiIntegrationTests(LocalApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task Local_migration_persists_opt_in_prices_and_immutable_demo_order_snapshots()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NovaDbContext>();
        var suffix = Guid.NewGuid().ToString("N");
        var offer = new CommerceOffer
        {
            Slug = $"migration-offer-{suffix}", DraftName = "Migration offer", DraftMarkdown = "# Offer",
            DraftIsPurchasable = true, DraftPriceMinorUnits = 49_000
        };
        db.CommerceOffers.Add(offer);
        var revision = CommerceOfferRevision.FromDraft(offer, Guid.NewGuid(), DateTimeOffset.UtcNow);
        offer.LatestRevisionNumber = revision.Number;
        offer.PublishedRevisionId = revision.Id;
        offer.State = CommerceOfferState.Published;
        db.CommerceOfferRevisions.Add(revision);
        await db.SaveChangesAsync();

        var order = new CommerceOrder
        {
            OrderNumber = $"NH-DEMO-{suffix}", IdempotencyKey = Guid.NewGuid(),
            RequestFingerprint = new string('a', 64), TotalMinorUnits = 98_000
        };
        db.CommerceOrders.Add(order);
        db.CommerceOrderLines.Add(new CommerceOrderLine
        {
            OrderId = order.Id, OfferId = offer.Id, OfferRevisionId = revision.Id, RevisionNumber = 1,
            OfferSlug = offer.Slug, OfferName = offer.DraftName, Quantity = 2,
            UnitPriceMinorUnits = 49_000, LineTotalMinorUnits = 98_000
        });
        db.CommercePayments.Add(new CommercePayment
        {
            OrderId = order.Id, AmountMinorUnits = 98_000
        });
        await db.SaveChangesAsync();

        var persisted = await db.CommerceOrders.AsNoTracking().SingleAsync(x => x.Id == order.Id);
        Assert.Equal(98_000, persisted.TotalMinorUnits);
        Assert.Equal("VND", persisted.CurrencyCode);
        Assert.Equal(1, await db.CommerceOrderLines.CountAsync(x => x.OrderId == order.Id));
        Assert.Equal(CommercePaymentStatus.Simulated,
            (await db.CommercePayments.AsNoTracking().SingleAsync(x => x.OrderId == order.Id)).Status);
    }

    [Fact]
    public async Task Checkout_prices_from_published_revision_and_same_key_cannot_duplicate_or_change_cart()
    {
        using var admin = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var slug = await CreateOfferAsync(admin, publish: true);
        using var visitor = factory.CreateClient();
        using var published = await visitor.GetAsync($"/api/v1/commerce/offers/{slug}");
        Assert.Equal(HttpStatusCode.OK, published.StatusCode);
        using var publishedJson = JsonDocument.Parse(await published.Content.ReadAsStreamAsync());
        Assert.Equal(1, publishedJson.RootElement.GetProperty("revision").GetInt32());
        Assert.True(publishedJson.RootElement.GetProperty("isPurchasable").GetBoolean());
        Assert.Equal(49_000, publishedJson.RootElement.GetProperty("priceMinorUnits").GetInt64());
        Assert.Equal("VND", publishedJson.RootElement.GetProperty("currencyCode").GetString());
        var key = Guid.NewGuid();

        using var first = await SendCheckoutAsync(visitor, key, slug, 2, clientPriceMinorUnits: 1);
        Assert.Equal(HttpStatusCode.Created, first.StatusCode);
        using var firstJson = JsonDocument.Parse(await first.Content.ReadAsStreamAsync());
        var orderNumber = firstJson.RootElement.GetProperty("orderNumber").GetString();
        Assert.Equal(98_000, firstJson.RootElement.GetProperty("totalMinorUnits").GetInt64());
        Assert.Equal("VND", firstJson.RootElement.GetProperty("currencyCode").GetString());
        Assert.Equal("simulated", firstJson.RootElement.GetProperty("paymentStatus").GetString());
        Assert.Equal("localDemo", firstJson.RootElement.GetProperty("paymentMethod").GetString());
        Assert.False(firstJson.RootElement.GetProperty("realCharge").GetBoolean());
        Assert.Equal("none", firstJson.RootElement.GetProperty("fulfilment").GetString());
        Assert.Equal(49_000, firstJson.RootElement.GetProperty("items")[0].GetProperty("unitPriceMinorUnits").GetInt64());

        using var replay = await SendCheckoutAsync(visitor, key, slug, 2, clientPriceMinorUnits: 1);
        Assert.Equal(HttpStatusCode.OK, replay.StatusCode);
        using var replayJson = JsonDocument.Parse(await replay.Content.ReadAsStreamAsync());
        Assert.Equal(orderNumber, replayJson.RootElement.GetProperty("orderNumber").GetString());
        Assert.True(replayJson.RootElement.GetProperty("idempotentReplay").GetBoolean());

        using var keyReuse = await SendCheckoutAsync(visitor, key, slug, 3, clientPriceMinorUnits: 1);
        Assert.Equal(HttpStatusCode.Conflict, keyReuse.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NovaDbContext>();
        Assert.Equal(1, await db.CommerceOrders.CountAsync(x => x.IdempotencyKey == key));
        var stored = await db.CommerceOrders.SingleAsync(x => x.IdempotencyKey == key);
        Assert.Equal(98_000, stored.TotalMinorUnits);
        Assert.Equal(1, await db.CommercePayments.CountAsync(x => x.OrderId == stored.Id));
        Assert.Equal(1, await db.CommerceOrderLines.CountAsync(x => x.OrderId == stored.Id));

        using var history = await admin.GetAsync("/api/v1/admin/commerce/orders?page=1&pageSize=20");
        Assert.Equal(HttpStatusCode.OK, history.StatusCode);
        using var historyJson = JsonDocument.Parse(await history.Content.ReadAsStreamAsync());
        var savedOrder = historyJson.RootElement.GetProperty("items").EnumerateArray()
            .Single(x => x.GetProperty("orderNumber").GetString() == orderNumber);
        Assert.Equal("simulated", savedOrder.GetProperty("paymentStatus").GetString());
        Assert.False(savedOrder.GetProperty("realCharge").GetBoolean());
        Assert.Equal("none", savedOrder.GetProperty("fulfilment").GetString());
    }

    [Fact]
    public async Task Published_slug_remains_public_and_checkout_uses_it_until_draft_is_republished()
    {
        using var admin = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var publishedSlug = await CreateOfferAsync(admin, publish: true);
        var draftSlug = $"replacement-{Guid.NewGuid():N}";
        await ChangeDraftSlugAsync(admin, publishedSlug, draftSlug);
        using var visitor = factory.CreateClient();

        using var detail = await visitor.GetAsync($"/api/v1/commerce/offers/{publishedSlug}");
        Assert.Equal(HttpStatusCode.OK, detail.StatusCode);
        using var detailJson = JsonDocument.Parse(await detail.Content.ReadAsStreamAsync());
        Assert.Equal(publishedSlug, detailJson.RootElement.GetProperty("slug").GetString());
        Assert.Equal(1, detailJson.RootElement.GetProperty("revision").GetInt32());
        using var unpublishedDraftPath = await visitor.GetAsync($"/api/v1/commerce/offers/{draftSlug}");
        Assert.Equal(HttpStatusCode.NotFound, unpublishedDraftPath.StatusCode);

        var list = await visitor.GetFromJsonAsync<JsonElement>("/api/v1/commerce/offers?page=1&pageSize=50");
        Assert.Contains(list.GetProperty("items").EnumerateArray(),
            item => item.GetProperty("slug").GetString() == publishedSlug);

        using var checkout = await SendCheckoutAsync(visitor, Guid.NewGuid(), publishedSlug, 1);
        Assert.Equal(HttpStatusCode.Created, checkout.StatusCode);
    }

    [Fact]
    public async Task Publish_rejects_a_slug_still_used_by_another_published_revision()
    {
        using var admin = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var occupiedSlug = await CreateOfferAsync(admin, publish: true);
        await ChangeDraftSlugAsync(admin, occupiedSlug, $"replacement-{Guid.NewGuid():N}");
        var duplicateSlug = await CreateOfferAsync(admin, publish: false, requestedSlug: occupiedSlug);
        using var publish = await PublishOfferAsync(admin, duplicateSlug);

        Assert.Equal(HttpStatusCode.Conflict, publish.StatusCode);
    }

    [Fact]
    public async Task Checkout_rejects_unpublished_offer_and_invalid_cart_without_partial_records()
    {
        using var admin = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var slug = await CreateOfferAsync(admin, publish: false);
        using var visitor = factory.CreateClient();
        var key = Guid.NewGuid();

        using var unavailable = await SendCheckoutAsync(visitor, key, slug, 1);
        Assert.Equal(HttpStatusCode.Conflict, unavailable.StatusCode);

        using var empty = new HttpRequestMessage(HttpMethod.Post, "/api/v1/commerce/orders")
        {
            Content = JsonContent.Create(new { items = Array.Empty<object>() })
        };
        empty.Headers.Add("Idempotency-Key", Guid.NewGuid().ToString());
        using var invalid = await visitor.SendAsync(empty);
        Assert.Equal(HttpStatusCode.BadRequest, invalid.StatusCode);

        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NovaDbContext>();
        Assert.Empty(await db.CommerceOrders.Where(x => x.IdempotencyKey == key).ToListAsync());
        Assert.Empty(await db.CommercePayments.ToListAsync());
    }

    [Fact]
    public async Task Checkout_rejects_published_offer_that_is_not_opted_in_for_purchase()
    {
        using var admin = await factory.CreateAuthenticatedClientAsync(factory.AdminEmail);
        var slug = await CreateOfferAsync(admin, publish: true, purchasable: false);
        using var visitor = factory.CreateClient();
        using var unavailable = await SendCheckoutAsync(visitor, Guid.NewGuid(), slug, 1);
        Assert.Equal(HttpStatusCode.Conflict, unavailable.StatusCode);
    }

    [Fact]
    public async Task Only_admin_can_read_order_history_and_public_order_lookup_is_absent()
    {
        using var anonymous = factory.CreateClient();
        using var unauthenticated = await anonymous.GetAsync("/api/v1/admin/commerce/orders");
        Assert.Equal(HttpStatusCode.Unauthorized, unauthenticated.StatusCode);

        using var editor = await factory.CreateAuthenticatedClientAsync(factory.EditorEmail);
        using var forbidden = await editor.GetAsync("/api/v1/admin/commerce/orders");
        Assert.Equal(HttpStatusCode.Forbidden, forbidden.StatusCode);

        using var publicLookup = await anonymous.GetAsync($"/api/v1/commerce/orders/{Guid.NewGuid():N}");
        Assert.Equal(HttpStatusCode.NotFound, publicLookup.StatusCode);
    }

    private async Task<string> CreateOfferAsync(HttpClient admin, bool publish, bool purchasable = true, string? requestedSlug = null)
    {
        var suffix = Guid.NewGuid().ToString("N");
        var slug = requestedSlug ?? $"local-demo-{suffix}";
        var csrf = await LocalApiFactory.GetCsrfAsync(admin);
        using var create = await LocalApiFactory.SendJsonAsync(admin, HttpMethod.Post, "/api/v1/admin/commerce/offers",
            new
            {
                name = "Local demo offer", slug, summary = "Offer used only by a local test.", markdown = "# Demo",
                kind = "cosmetic", displayPrice = "49.000 ₫", isPurchasable = purchasable,
                priceMinorUnits = purchasable ? 49_000L : (long?)null
            }, csrf);
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        if (publish)
        {
            var id = await ReadGuidAsync(create, "id");
            using var current = await admin.GetAsync($"/api/v1/admin/commerce/offers/{id}");
            current.EnsureSuccessStatusCode();
            csrf = await LocalApiFactory.GetCsrfAsync(admin);
            using var publishResponse = await LocalApiFactory.SendJsonAsync(admin, HttpMethod.Post,
                $"/api/v1/admin/commerce/offers/{id}/publish", new { }, csrf, current.Headers.ETag?.ToString());
            publishResponse.EnsureSuccessStatusCode();
        }
        return slug;
    }

    private static async Task<Guid> FindOfferIdAsync(HttpClient admin, string slug)
    {
        var offers = await admin.GetFromJsonAsync<JsonElement>("/api/v1/admin/commerce/offers");
        return offers.EnumerateArray().Single(x => x.GetProperty("slug").GetString() == slug)
            .GetProperty("id").GetGuid();
    }

    private static async Task ChangeDraftSlugAsync(HttpClient admin, string slug, string replacement)
    {
        var id = await FindOfferIdAsync(admin, slug);
        using var current = await admin.GetAsync($"/api/v1/admin/commerce/offers/{id}");
        current.EnsureSuccessStatusCode();
        var csrf = await LocalApiFactory.GetCsrfAsync(admin);
        using var update = await LocalApiFactory.SendJsonAsync(admin, HttpMethod.Patch,
            $"/api/v1/admin/commerce/offers/{id}",
            new
            {
                name = "Local demo offer", slug = replacement, summary = "Offer used only by a local test.",
                markdown = "# Demo", kind = "cosmetic", displayPrice = "49.000 ₫", providerProductCode = (string?)null,
                isPurchasable = true, priceMinorUnits = 49_000L
            }, csrf, current.Headers.ETag?.ToString());
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
    }

    private static async Task<HttpResponseMessage> PublishOfferAsync(HttpClient admin, string slug)
    {
        var id = await FindOfferIdAsync(admin, slug);
        using var current = await admin.GetAsync($"/api/v1/admin/commerce/offers/{id}");
        current.EnsureSuccessStatusCode();
        var csrf = await LocalApiFactory.GetCsrfAsync(admin);
        return await LocalApiFactory.SendJsonAsync(admin, HttpMethod.Post,
            $"/api/v1/admin/commerce/offers/{id}/publish", new { }, csrf, current.Headers.ETag?.ToString());
    }

    private static async Task<HttpResponseMessage> SendCheckoutAsync(
        HttpClient client, Guid key, string slug, int quantity, long? clientPriceMinorUnits = null)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, "/api/v1/commerce/orders")
        {
            Content = JsonContent.Create(new
            {
                items = new[] { new { slug, quantity, priceMinorUnits = clientPriceMinorUnits } }
            })
        };
        request.Headers.Add("Idempotency-Key", key.ToString());
        return await client.SendAsync(request);
    }

    private static async Task<Guid> ReadGuidAsync(HttpResponseMessage response, string property)
    {
        using var document = JsonDocument.Parse(await response.Content.ReadAsStreamAsync());
        return document.RootElement.GetProperty(property).GetGuid();
    }
}
