using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Api.Infrastructure;
using NovaHaven.Application.Catalog;
using NovaHaven.Domain.Catalog;
using NovaHaven.Domain.Catalog.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Api.Endpoints;

public static class CatalogEndpoints
{
    public static void MapCatalogEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/admin/catalog/items").RequireAuthorization("AdminOnly");
        admin.AddEndpointFilter(async (context, next) =>
        {
            if (HttpMethods.IsGet(context.HttpContext.Request.Method)) return await next(context);
            var antiforgery = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
            return await AuthEndpoints.ValidateCsrf(context.HttpContext, antiforgery)
                ? await next(context)
                : Results.Problem(statusCode: 400, title: "Invalid anti-forgery token.");
        });
        admin.MapGet("", AdminListAsync);
        admin.MapPost("", CreateAsync);
        admin.MapGet("/{id:guid}", AdminDetailAsync);
        admin.MapPatch("/{id:guid}", UpdateAsync);
        admin.MapPost("/{id:guid}/publish", PublishAsync);
        admin.MapPost("/{id:guid}/unpublish", UnpublishAsync);

        var publicGroup = app.MapGroup("/api/v1/catalog/items");
        publicGroup.MapGet("", ListAsync);
        publicGroup.MapGet("/{slug}", DetailAsync);
    }

    private static async Task<IResult> ListAsync(
        NovaDbContext db,
        string? q,
        string? kind,
        int? page,
        int? pageSize,
        CancellationToken ct)
    {
        var currentPage = page ?? 1;
        var size = pageSize ?? 20;
        if (currentPage < 1 || size is < 1 or > 50 || (q?.Length ?? 0) > 100)
            return Results.Problem(statusCode: 400, title: "Invalid catalog pagination or filter.");
        if (!TryParseKind(kind, out var filterKind))
            return Results.Problem(statusCode: 400, title: "Invalid catalog item kind.");

        var query = from item in db.CatalogItems.AsNoTracking()
                    join revision in db.CatalogItemRevisions.AsNoTracking()
                        on item.PublishedRevisionId equals (Guid?)revision.Id
                    where item.State == CatalogItemState.Published
                    select new { Item = item, Revision = revision };
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.Revision.Name.Contains(term) || x.Revision.Summary.Contains(term));
        }
        if (filterKind.HasValue) query = query.Where(x => x.Revision.Kind == filterKind.Value);

        var total = await query.CountAsync(ct);
        var offset = ((long)currentPage - 1) * size;
        if (offset > int.MaxValue) return Results.Problem(statusCode: 400, title: "Page is out of range.");
        var rows = await query.OrderBy(x => x.Revision.Name).ThenBy(x => x.Item.Id)
            .Skip((int)offset).Take(size)
            .Select(x => new
            {
                id = x.Item.Id,
                slug = x.Item.Slug,
                name = x.Revision.Name,
                summary = x.Revision.Summary,
                kind = x.Revision.Kind,
                revision = x.Revision.Number,
                publishedAt = x.Revision.PublishedAt,
                updatedAt = x.Item.UpdatedAt
            }).ToListAsync(ct);
        var items = rows.Select(x => new
        {
            x.id, x.slug, x.name, x.summary,
            kind = KindName(x.kind), x.revision, x.publishedAt, x.updatedAt
        });
        return Results.Ok(new { items, page = currentPage, pageSize = size, total });
    }

    private static async Task<IResult> DetailAsync(NovaDbContext db, string slug, CancellationToken ct)
    {
        var row = await (from item in db.CatalogItems.AsNoTracking()
                         join revision in db.CatalogItemRevisions.AsNoTracking()
                             on item.PublishedRevisionId equals (Guid?)revision.Id
                         where item.State == CatalogItemState.Published && item.Slug == slug
                         select new { Item = item, Revision = revision }).SingleOrDefaultAsync(ct);
        if (row is null) return Results.NotFound();
        return Results.Ok(new
        {
            id = row.Item.Id,
            slug = row.Item.Slug,
            name = row.Revision.Name,
            summary = row.Revision.Summary,
            markdown = row.Revision.Markdown,
            kind = KindName(row.Revision.Kind),
            revision = row.Revision.Number,
            publishedAt = row.Revision.PublishedAt,
            updatedAt = row.Item.UpdatedAt
        });
    }

    private static async Task<IResult> AdminListAsync(NovaDbContext db, CancellationToken ct)
    {
        var rows = await db.CatalogItems.AsNoTracking().OrderByDescending(x => x.UpdatedAt).Take(100).ToListAsync(ct);
        return Results.Ok(rows.Select(x => new
        {
            x.Id, x.Slug, name = x.DraftName, kind = KindName(x.DraftKind), state = x.State.ToString().ToLowerInvariant(),
            x.LatestRevisionNumber, x.UpdatedAt, etag = ETag(x)
        }));
    }

    private static async Task<IResult> CreateAsync(
        NovaDbContext db,
        HttpContext http,
        CatalogItemRequest request,
        CancellationToken ct)
    {
        if (!TryInput(request, out var input, out var parseError)) return parseError!;
        var errors = CatalogItemValidator.Validate(input);
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        if (await db.CatalogItems.AnyAsync(x => x.Slug == input.Slug, ct)) return Conflict("Catalog slug already exists.");
        var item = new GameCatalogItem
        {
            Slug = input.Slug,
            DraftName = input.Name.Trim(),
            DraftSummary = input.Summary ?? "",
            DraftMarkdown = input.Markdown,
            DraftKind = input.Kind,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.CatalogItems.Add(item);
        AuditWriter.TryAdd(db, http, "catalog_item.created", "GameCatalogItem", item.Id, new { item.Slug });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (UniqueViolation(e)) { return Conflict("Catalog slug already exists."); }
        return Results.Created($"/api/v1/admin/catalog/items/{item.Id}", new { item.Id, item.Slug, etag = ETag(item) });
    }

    private static async Task<IResult> AdminDetailAsync(Guid id, NovaDbContext db, HttpContext http, CancellationToken ct)
    {
        var item = await db.CatalogItems.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return Results.NotFound();
        var published = item.PublishedRevisionId is Guid revisionId
            ? await db.CatalogItemRevisions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == revisionId, ct)
            : null;
        http.Response.Headers.ETag = ETag(item);
        http.Response.Headers.CacheControl = "no-store";
        return Results.Ok(new
        {
            item.Id, item.Slug, name = item.DraftName, summary = item.DraftSummary,
            markdown = item.DraftMarkdown, kind = KindName(item.DraftKind), state = item.State.ToString().ToLowerInvariant(),
            item.LatestRevisionNumber,
            published = published is null ? null : new
            {
                published.Id, published.Number, name = published.Name, summary = published.Summary,
                markdown = published.Markdown, kind = KindName(published.Kind), published.PublishedAt
            }
        });
    }

    private static async Task<IResult> UpdateAsync(
        Guid id,
        NovaDbContext db,
        HttpContext http,
        CatalogItemRequest request,
        CancellationToken ct)
    {
        var item = await db.CatalogItems.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return Results.NotFound();
        var precondition = CheckPrecondition(http, item);
        if (precondition is not null) return precondition;
        if (!TryInput(request, out var input, out var parseError)) return parseError!;
        var errors = CatalogItemValidator.Validate(input);
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        if (input.Slug != item.Slug && await db.CatalogItems.AnyAsync(x => x.Slug == input.Slug && x.Id != id, ct))
            return Conflict("Catalog slug already exists.");
        item.Slug = input.Slug;
        item.DraftName = input.Name.Trim();
        item.DraftSummary = input.Summary ?? "";
        item.DraftMarkdown = input.Markdown;
        item.DraftKind = input.Kind;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        AuditWriter.TryAdd(db, http, "catalog_item.edited", "GameCatalogItem", item.Id, new { item.Slug });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Stale(); }
        catch (DbUpdateException e) when (UniqueViolation(e)) { return Conflict("Catalog slug already exists."); }
        http.Response.Headers.ETag = ETag(item);
        return Results.Ok(new { item.Id, etag = ETag(item) });
    }

    private static async Task<IResult> PublishAsync(Guid id, NovaDbContext db, HttpContext http, CancellationToken ct)
    {
        var item = await db.CatalogItems.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return Results.NotFound();
        var precondition = CheckPrecondition(http, item);
        if (precondition is not null) return precondition;
        var errors = CatalogItemValidator.Validate(new CatalogItemInput(
            item.DraftName, item.Slug, item.DraftSummary, item.DraftMarkdown, item.DraftKind));
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        var actor = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(actor, out var actorId)) return Results.Unauthorized();
        var now = DateTimeOffset.UtcNow;
        var revision = GameCatalogItemRevision.FromDraft(item, actorId, now);
        item.State = CatalogItemState.Published;
        item.WasPublished = true;
        item.PublishedRevisionId = revision.Id;
        item.LatestRevisionNumber = revision.Number;
        item.UpdatedAt = now;
        db.CatalogItemRevisions.Add(revision);
        AuditWriter.TryAdd(db, http, "catalog_item.published", "GameCatalogItem", item.Id,
            new { revision = revision.Number });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Stale(); }
        http.Response.Headers.ETag = ETag(item);
        return Results.Ok(new { item.Id, revisionId = revision.Id, revision.Number, publishedAt = revision.PublishedAt, etag = ETag(item) });
    }

    private static async Task<IResult> UnpublishAsync(Guid id, NovaDbContext db, HttpContext http, CancellationToken ct)
    {
        var item = await db.CatalogItems.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (item is null) return Results.NotFound();
        var precondition = CheckPrecondition(http, item);
        if (precondition is not null) return precondition;
        if (item.State != CatalogItemState.Published) return Conflict("Catalog item is not currently published.");
        item.State = CatalogItemState.Unpublished;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        AuditWriter.TryAdd(db, http, "catalog_item.unpublished", "GameCatalogItem", item.Id);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Stale(); }
        http.Response.Headers.ETag = ETag(item);
        return Results.NoContent();
    }

    private static bool TryInput(CatalogItemRequest request, out CatalogItemInput input, out IResult? error)
    {
        if (!Enum.TryParse<CatalogItemKind>(request.Kind, true, out var kind) || !Enum.IsDefined(kind))
        {
            input = default!;
            error = Results.ValidationProblem(new Dictionary<string, string[]> { ["kind"] = ["Kind is not supported."] });
            return false;
        }
        input = new CatalogItemInput(request.Name ?? "", request.Slug ?? "", request.Summary ?? "", request.Markdown ?? "", kind);
        error = null;
        return true;
    }

    private static bool TryParseKind(string? value, out CatalogItemKind? kind)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            kind = null;
            return true;
        }
        if (Enum.TryParse<CatalogItemKind>(value, true, out var parsed) && Enum.IsDefined(parsed))
        {
            kind = parsed;
            return true;
        }
        kind = null;
        return false;
    }
    private static string KindName(CatalogItemKind kind) => kind.ToString().ToLowerInvariant();
    private static string ETag(GameCatalogItem item) => $"\"{Convert.ToBase64String(item.RowVersion)}\"";
    private static IResult? CheckPrecondition(HttpContext http, GameCatalogItem item)
    {
        if (!http.Request.Headers.TryGetValue("If-Match", out var value) || string.IsNullOrWhiteSpace(value.ToString()))
            return Results.Problem(statusCode: 428, title: "If-Match is required.");
        return string.Equals(value.ToString(), ETag(item), StringComparison.Ordinal) ? null : Stale();
    }
    private static IResult Stale() => Results.Problem(statusCode: 412, title: "Catalog item changed; reload before editing.");
    private static IResult Conflict(string title) => Results.Problem(statusCode: 409, title: title);
    private static bool UniqueViolation(DbUpdateException exception) => exception.InnerException is SqlException { Number: 2601 or 2627 };
}

public sealed record CatalogItemRequest(string? Name, string? Slug, string? Summary, string? Markdown, string? Kind);
