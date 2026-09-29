using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Api.Infrastructure;
using NovaHaven.Application.News;
using NovaHaven.Domain.News;
using NovaHaven.Domain.News.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Api.Endpoints;

public static class NewsEndpoints
{
    public static void MapNewsEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/admin/news").RequireAuthorization("AdminOnly");
        admin.AddEndpointFilter(async (context, next) =>
        {
            if (HttpMethods.IsGet(context.HttpContext.Request.Method)) return await next(context);
            var antiforgery = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
            return await AuthEndpoints.ValidateCsrf(context.HttpContext, antiforgery)
                ? await next(context)
                : Results.Problem(statusCode: 400, title: "Invalid anti-forgery token.");
        });
        admin.MapGet("", async (NovaDbContext db, CancellationToken ct) => Results.Ok(await db.NewsPosts.AsNoTracking()
            .OrderByDescending(x => x.UpdatedAt).Take(100)
            .Select(x => new { x.Id, x.Slug, title = x.DraftTitle, x.State, x.PublishedAt, x.UpdatedAt, etag = ETag(x) })
            .ToListAsync(ct)));
        admin.MapPost("", CreateAsync);
        admin.MapGet("/{id:guid}", GetAdminAsync);
        admin.MapPatch("/{id:guid}", UpdateAsync);
        admin.MapPost("/{id:guid}/publish", PublishAsync);
        admin.MapPost("/{id:guid}/unpublish", UnpublishAsync);

        var publicGroup = app.MapGroup("/api/v1/news");
        publicGroup.MapGet("", ListAsync);
        publicGroup.MapGet("/{slug}", DetailAsync);
    }

    private static async Task<IResult> ListAsync(NovaDbContext db, string? q, int? page, int? pageSize, CancellationToken ct)
    {
        var currentPage = page ?? 1;
        var size = pageSize ?? 20;
        if (currentPage < 1 || size is < 1 or > 50 || (q?.Length ?? 0) > 100)
            return Results.Problem(statusCode: 400, title: "Invalid news pagination or filter.");
        var query = db.NewsPosts.AsNoTracking().Where(x => x.State == NewsState.Published);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.DraftTitle.Contains(term) || x.DraftSummary.Contains(term));
        }
        var total = await query.CountAsync(ct);
        var offset = ((long)currentPage - 1) * size;
        if (offset > int.MaxValue) return Results.Problem(statusCode: 400, title: "Page is out of range.");
        var items = await query.OrderByDescending(x => x.PublishedAt).ThenByDescending(x => x.Id)
            .Skip((int)offset).Take(size)
            .Select(x => new { id = x.Id, slug = x.Slug, title = x.DraftTitle, summary = x.DraftSummary,
                markdown = x.DraftMarkdown, x.PublishedAt, x.UpdatedAt }).ToListAsync(ct);
        return Results.Ok(new { items, page = currentPage, pageSize = size, total });
    }

    private static async Task<IResult> DetailAsync(NovaDbContext db, string slug, CancellationToken ct)
    {
        var item = await db.NewsPosts.AsNoTracking().Where(x => x.State == NewsState.Published && x.Slug == slug)
            .Select(x => new { x.Id, x.Slug, title = x.DraftTitle, summary = x.DraftSummary,
                markdown = x.DraftMarkdown, x.PublishedAt, x.UpdatedAt }).SingleOrDefaultAsync(ct);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    private static async Task<IResult> CreateAsync(NovaDbContext db, HttpContext http, NewsPostInput input, CancellationToken ct)
    {
        var errors = NewsValidator.Validate(input);
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        if (await db.NewsPosts.AnyAsync(x => x.Slug == input.Slug, ct)) return Conflict("News slug already exists.");
        var post = new NewsPost { Slug = input.Slug, DraftTitle = input.Title.Trim(), DraftSummary = input.Summary,
            DraftMarkdown = input.Markdown, UpdatedAt = DateTimeOffset.UtcNow };
        db.NewsPosts.Add(post);
        AuditWriter.TryAdd(db, http, "news.created", "NewsPost", post.Id, new { post.Slug });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (UniqueViolation(e)) { return Conflict("News slug already exists."); }
        return Results.Created($"/api/v1/admin/news/{post.Id}", new { post.Id, post.Slug, etag = ETag(post) });
    }

    private static async Task<IResult> GetAdminAsync(Guid id, NovaDbContext db, HttpContext http, CancellationToken ct)
    {
        var post = await db.NewsPosts.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (post is null) return Results.NotFound();
        http.Response.Headers.ETag = ETag(post);
        http.Response.Headers.CacheControl = "no-store";
        return Results.Ok(new { post.Id, post.Slug, title = post.DraftTitle, summary = post.DraftSummary,
            markdown = post.DraftMarkdown, post.State, post.PublishedAt });
    }

    private static async Task<IResult> UpdateAsync(Guid id, NovaDbContext db, HttpContext http, NewsPostInput input, CancellationToken ct)
    {
        var post = await db.NewsPosts.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (post is null) return Results.NotFound();
        var precondition = CheckPrecondition(http, post);
        if (precondition is not null) return precondition;
        var errors = NewsValidator.Validate(input);
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        if (input.Slug != post.Slug && await db.NewsPosts.AnyAsync(x => x.Slug == input.Slug && x.Id != id, ct))
            return Conflict("News slug already exists.");
        post.Slug = input.Slug;
        post.DraftTitle = input.Title.Trim();
        post.DraftSummary = input.Summary;
        post.DraftMarkdown = input.Markdown;
        post.UpdatedAt = DateTimeOffset.UtcNow;
        AuditWriter.TryAdd(db, http, "news.edited", "NewsPost", post.Id, new { post.Slug });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Stale(); }
        catch (DbUpdateException e) when (UniqueViolation(e)) { return Conflict("News slug already exists."); }
        http.Response.Headers.ETag = ETag(post);
        return Results.Ok(new { post.Id, etag = ETag(post) });
    }

    private static async Task<IResult> PublishAsync(Guid id, NovaDbContext db, HttpContext http, CancellationToken ct)
    {
        var post = await db.NewsPosts.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (post is null) return Results.NotFound();
        var precondition = CheckPrecondition(http, post);
        if (precondition is not null) return precondition;
        var actor = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(actor, out var actorId)) return Results.Unauthorized();
        post.State = NewsState.Published;
        post.PublishedAt = DateTimeOffset.UtcNow;
        post.PublishedBy = actorId;
        post.UpdatedAt = post.PublishedAt.Value;
        AuditWriter.TryAdd(db, http, "news.published", "NewsPost", post.Id);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Stale(); }
        http.Response.Headers.ETag = ETag(post);
        return Results.Ok(new { post.Id, post.PublishedAt, etag = ETag(post) });
    }

    private static async Task<IResult> UnpublishAsync(Guid id, NovaDbContext db, HttpContext http, CancellationToken ct)
    {
        var post = await db.NewsPosts.SingleOrDefaultAsync(x => x.Id == id, ct);
        if (post is null) return Results.NotFound();
        var precondition = CheckPrecondition(http, post);
        if (precondition is not null) return precondition;
        if (post.State != NewsState.Published) return Conflict("News post is not currently published.");
        post.State = NewsState.Unpublished;
        post.UpdatedAt = DateTimeOffset.UtcNow;
        AuditWriter.TryAdd(db, http, "news.unpublished", "NewsPost", post.Id);
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Stale(); }
        return Results.NoContent();
    }

    private static string ETag(NewsPost post) => $"\"{Convert.ToBase64String(post.RowVersion)}\"";
    private static IResult? CheckPrecondition(HttpContext http, NewsPost post)
    {
        if (!http.Request.Headers.TryGetValue("If-Match", out var value) || string.IsNullOrWhiteSpace(value.ToString()))
            return Results.Problem(statusCode: 428, title: "If-Match is required.");
        return string.Equals(value.ToString(), ETag(post), StringComparison.Ordinal) ? null : Stale();
    }
    private static IResult Stale() => Results.Problem(statusCode: 412, title: "News post changed; reload before editing.");
    private static IResult Conflict(string title) => Results.Problem(statusCode: 409, title: title);
    private static bool UniqueViolation(DbUpdateException exception) => exception.InnerException is SqlException { Number: 2601 or 2627 };
}
