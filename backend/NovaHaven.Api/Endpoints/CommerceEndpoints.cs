using System.Data;
using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Api.Infrastructure;
using NovaHaven.Application.Commerce;
using NovaHaven.Domain.Commerce;
using NovaHaven.Domain.Commerce.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Api.Endpoints;

public static class CommerceEndpoints
{
    public static void MapCommerceEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/admin/commerce/offers").RequireAuthorization("AdminOnly"); admin.AddEndpointFilter(CsrfFilter);
        admin.MapGet("", ListAdminAsync); admin.MapPost("", CreateAsync); admin.MapGet("/{id:guid}", GetAdminAsync); admin.MapPatch("/{id:guid}", UpdateAsync); admin.MapPost("/{id:guid}/publish", PublishAsync); admin.MapPost("/{id:guid}/unpublish", UnpublishAsync);
        var publicGroup = app.MapGroup("/api/v1/commerce/offers"); publicGroup.MapGet("", ListAsync); publicGroup.MapGet("/{slug}", DetailAsync);
    }
    private static async ValueTask<object?> CsrfFilter(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    { if (HttpMethods.IsGet(context.HttpContext.Request.Method)) return await next(context); var antiforgery = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>(); return await AuthEndpoints.ValidateCsrf(context.HttpContext, antiforgery) ? await next(context) : Results.Problem(statusCode: 400, title: "Invalid anti-forgery token."); }
    private static async Task<IResult> ListAdminAsync(NovaDbContext db, CancellationToken ct) => Results.Ok(await db.CommerceOffers.AsNoTracking().OrderByDescending(x => x.UpdatedAt).Take(100).Select(x => new { x.Id, x.Slug, name = x.DraftName, kind = KindName(x.DraftKind), x.State, x.LatestRevisionNumber, x.UpdatedAt, definitionOnly = true, isPurchasable = x.DraftIsPurchasable, priceMinorUnits = x.DraftPriceMinorUnits, etag = ETag(x) }).ToListAsync(ct));
    private static async Task<IResult> ListAsync(NovaDbContext db, string? q, int? page, int? pageSize, CancellationToken ct)
    {
        var currentPage = page ?? 1;
        var size = pageSize ?? 20;
        if (currentPage < 1 || size is < 1 or > 50 || (q?.Length ?? 0) > 100)
            return Results.Problem(statusCode: 400, title: "Invalid commerce pagination or filter.");
        var query = from offer in db.CommerceOffers.AsNoTracking()
                    join revision in db.CommerceOfferRevisions.AsNoTracking() on offer.PublishedRevisionId equals revision.Id
                    where offer.State == CommerceOfferState.Published
                    select new { offer, revision };
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.revision.Name.Contains(term) || x.revision.Summary.Contains(term));
        }
        var total = await query.CountAsync(ct);
        var offset = ((long)currentPage - 1) * size;
        if (offset > int.MaxValue) return Results.Problem(statusCode: 400, title: "Page is out of range.");
        var items = await query.OrderByDescending(x => x.revision.PublishedAt).Skip((int)offset).Take(size)
            .Select(x => new
            {
                id = x.offer.Id, slug = x.revision.Slug, name = x.revision.Name, summary = x.revision.Summary,
                kind = KindName(x.revision.Kind), revision = x.revision.Number, definitionOnly = true,
                isPurchasable = x.revision.IsPurchasable, priceMinorUnits = x.revision.PriceMinorUnits,
                currencyCode = "VND", checkoutMode = x.revision.IsPurchasable ? "localDemo" : (string?)null,
                updatedAt = x.revision.PublishedAt
            }).ToListAsync(ct);
        return Results.Ok(new { items, page = currentPage, pageSize = size, total });
    }
    private static async Task<IResult> DetailAsync(NovaDbContext db, string slug, CancellationToken ct)
    {
        var item = await (from offer in db.CommerceOffers.AsNoTracking()
                          join revision in db.CommerceOfferRevisions.AsNoTracking() on offer.PublishedRevisionId equals revision.Id
                          where offer.State == CommerceOfferState.Published && revision.Slug == slug
                          select new
                          {
                              id = offer.Id, revision.Slug, name = revision.Name, summary = revision.Summary,
                              markdown = revision.Markdown, kind = KindName(revision.Kind), revision = revision.Number,
                              revision.DisplayPrice, revision.ProviderProductCode, isPurchasable = revision.IsPurchasable,
                              priceMinorUnits = revision.PriceMinorUnits, currencyCode = "VND",
                              checkoutMode = revision.IsPurchasable ? "localDemo" : (string?)null,
                              definitionOnly = true, revision.PublishedAt, updatedAt = revision.PublishedAt
                          }).SingleOrDefaultAsync(ct);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }
    private static async Task<IResult> CreateAsync(NovaDbContext db, HttpContext http, CommerceRequest request, CancellationToken ct)
    { var input = ToInput(request); var errors = CommerceValidator.Validate(input); if (errors.Count > 0) return Results.ValidationProblem(errors); if (await db.CommerceOffers.AnyAsync(x => x.Slug == input.Slug, ct)) return Conflict("Commerce offer slug already exists."); var item = new CommerceOffer { Slug = input.Slug, DraftName = input.Name.Trim(), DraftSummary = input.Summary, DraftMarkdown = input.Markdown, DraftKind = Enum.Parse<CommerceOfferKind>(input.Kind, true), DraftDisplayPrice = input.DisplayPrice.Trim(), DraftProviderProductCode = input.ProviderProductCode?.Trim(), DraftIsPurchasable = input.IsPurchasable, DraftPriceMinorUnits = input.PriceMinorUnits, UpdatedAt = DateTimeOffset.UtcNow }; db.CommerceOffers.Add(item); AuditWriter.TryAdd(db, http, "commerce.offer.created", "CommerceOffer", item.Id, new { item.Slug, definitionOnly = true, item.DraftIsPurchasable, item.DraftPriceMinorUnits }); try { await db.SaveChangesAsync(ct); } catch (DbUpdateException e) when (UniqueViolation(e)) { return Conflict("Commerce offer slug already exists."); } return Results.Created($"/api/v1/admin/commerce/offers/{item.Id}", new { item.Id, item.Slug, definitionOnly = true, etag = ETag(item) }); }
    private static async Task<IResult> GetAdminAsync(Guid id, NovaDbContext db, HttpContext http, CancellationToken ct)
    { var item = await db.CommerceOffers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return Results.NotFound(); http.Response.Headers.ETag = ETag(item); http.Response.Headers.CacheControl = "no-store"; return Results.Ok(new { item.Id, item.Slug, name = item.DraftName, summary = item.DraftSummary, markdown = item.DraftMarkdown, kind = KindName(item.DraftKind), displayPrice = item.DraftDisplayPrice, providerProductCode = item.DraftProviderProductCode, isPurchasable = item.DraftIsPurchasable, priceMinorUnits = item.DraftPriceMinorUnits, currencyCode = "VND", definitionOnly = true, item.State, item.LatestRevisionNumber }); }
    private static async Task<IResult> UpdateAsync(Guid id, NovaDbContext db, HttpContext http, CommerceRequest request, CancellationToken ct)
    { var item = await db.CommerceOffers.SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return Results.NotFound(); var precondition = CheckPrecondition(http, item); if (precondition is not null) return precondition; var input = ToInput(request); var errors = CommerceValidator.Validate(input); if (errors.Count > 0) return Results.ValidationProblem(errors); if (input.Slug != item.Slug && await db.CommerceOffers.AnyAsync(x => x.Slug == input.Slug && x.Id != id, ct)) return Conflict("Commerce offer slug already exists."); item.Slug = input.Slug; item.DraftName = input.Name.Trim(); item.DraftSummary = input.Summary; item.DraftMarkdown = input.Markdown; item.DraftKind = Enum.Parse<CommerceOfferKind>(input.Kind, true); item.DraftDisplayPrice = input.DisplayPrice.Trim(); item.DraftProviderProductCode = input.ProviderProductCode?.Trim(); item.DraftIsPurchasable = input.IsPurchasable; item.DraftPriceMinorUnits = input.PriceMinorUnits; item.UpdatedAt = DateTimeOffset.UtcNow; AuditWriter.TryAdd(db, http, "commerce.offer.edited", "CommerceOffer", item.Id, new { item.Slug, definitionOnly = true, item.DraftIsPurchasable, item.DraftPriceMinorUnits }); try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { return Stale(); } catch (DbUpdateException e) when (UniqueViolation(e)) { return Conflict("Commerce offer slug already exists."); } http.Response.Headers.ETag = ETag(item); return Results.Ok(new { item.Id, definitionOnly = true, etag = ETag(item) }); }
    private static async Task<IResult> PublishAsync(Guid id, NovaDbContext db, HttpContext http, CancellationToken ct)
    {
        var item = await db.CommerceOffers.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return Results.NotFound();
        var precondition = CheckPrecondition(http, item);
        if (precondition is not null) return precondition;
        if (!Guid.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Unauthorized();

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        var slugIsPublished = await (from offer in db.CommerceOffers.AsNoTracking()
                                     join revision in db.CommerceOfferRevisions.AsNoTracking() on offer.PublishedRevisionId equals revision.Id
                                     where offer.Id != id && offer.State == CommerceOfferState.Published && revision.Slug == item.Slug
                                     select offer.Id).AnyAsync(ct);
        if (slugIsPublished)
        {
            await transaction.RollbackAsync(ct);
            return Conflict("A published commerce offer already uses this slug.");
        }

        var published = CommerceOfferRevision.FromDraft(item, actorId, DateTimeOffset.UtcNow);
        db.CommerceOfferRevisions.Add(published);
        item.LatestRevisionNumber = published.Number;
        item.PublishedRevisionId = published.Id;
        item.State = CommerceOfferState.Published;
        item.WasPublished = true;
        item.UpdatedAt = published.PublishedAt;
        AuditWriter.TryAdd(db, http, "commerce.offer.published", "CommerceOffer", item.Id, new { published.Number, definitionOnly = true });
        try
        {
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateException exception) when (exception.InnerException is SqlException { Number: 1205 })
        {
            return Conflict("Concurrent publication prevented this slug from being published. Retry after reloading.");
        }
        catch (DbUpdateConcurrencyException)
        {
            return Stale();
        }
        catch (SqlException exception) when (exception.Number == 1205)
        {
            return Conflict("Concurrent publication prevented this slug from being published. Retry after reloading.");
        }

        http.Response.Headers.ETag = ETag(item);
        return Results.Ok(new { item.Id, revision = published.Number, definitionOnly = true, etag = ETag(item) });
    }
    private static async Task<IResult> UnpublishAsync(Guid id, NovaDbContext db, HttpContext http, CancellationToken ct)
    { var item = await db.CommerceOffers.SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return Results.NotFound(); var precondition = CheckPrecondition(http, item); if (precondition is not null) return precondition; if (item.State != CommerceOfferState.Published) return Conflict("Commerce offer is not currently published."); item.State = CommerceOfferState.Unpublished; item.UpdatedAt = DateTimeOffset.UtcNow; AuditWriter.TryAdd(db, http, "commerce.offer.unpublished", "CommerceOffer", item.Id); try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { return Stale(); } return Results.NoContent(); }
    private static CommerceInput ToInput(CommerceRequest request) => new(request.Name ?? "", request.Slug ?? "", request.Summary ?? "", request.Markdown ?? "", request.Kind ?? "", request.DisplayPrice ?? "", request.ProviderProductCode, request.IsPurchasable, request.PriceMinorUnits);
    private static string KindName(CommerceOfferKind kind) => kind.ToString().ToLowerInvariant(); private static string ETag(CommerceOffer item) => $"\"{Convert.ToBase64String(item.RowVersion)}\""; private static IResult? CheckPrecondition(HttpContext http, CommerceOffer item) => !http.Request.Headers.TryGetValue("If-Match", out var value) || string.IsNullOrWhiteSpace(value.ToString()) ? Results.Problem(statusCode: 428, title: "If-Match is required.") : string.Equals(value.ToString(), ETag(item), StringComparison.Ordinal) ? null : Stale(); private static IResult Stale() => Results.Problem(statusCode: 412, title: "Commerce offer changed; reload before editing."); private static IResult Conflict(string title) => Results.Problem(statusCode: 409, title: title); private static bool UniqueViolation(DbUpdateException exception) => exception.InnerException is SqlException { Number: 2601 or 2627 };
}

public sealed record CommerceRequest(string? Name, string? Slug, string? Summary, string? Markdown, string? Kind, string? DisplayPrice, string? ProviderProductCode, bool IsPurchasable = false, long? PriceMinorUnits = null);
