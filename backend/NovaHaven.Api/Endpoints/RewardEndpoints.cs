using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Api.Infrastructure;
using NovaHaven.Application.Rewards;
using NovaHaven.Domain.Rewards;
using NovaHaven.Domain.Rewards.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Api.Endpoints;

public static class RewardEndpoints
{
    public static void MapRewardEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/admin/rewards").RequireAuthorization("AdminOnly");
        admin.AddEndpointFilter(CsrfFilter);
        admin.MapGet("", ListAdminAsync);
        admin.MapPost("", CreateAsync);
        admin.MapGet("/{id:guid}", GetAdminAsync);
        admin.MapPatch("/{id:guid}", UpdateAsync);
        admin.MapPost("/{id:guid}/publish", PublishAsync);
        admin.MapPost("/{id:guid}/unpublish", UnpublishAsync);

        var publicGroup = app.MapGroup("/api/v1/rewards");
        publicGroup.MapGet("", ListAsync);
        publicGroup.MapGet("/{slug}", DetailAsync);
    }

    private static async ValueTask<object?> CsrfFilter(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        if (HttpMethods.IsGet(context.HttpContext.Request.Method)) return await next(context);
        var antiforgery = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
        return await AuthEndpoints.ValidateCsrf(context.HttpContext, antiforgery) ? await next(context) : Results.Problem(statusCode: 400, title: "Invalid anti-forgery token.");
    }

    private static async Task<IResult> ListAdminAsync(NovaDbContext db, CancellationToken ct) => Results.Ok(await db.RewardDefinitions.AsNoTracking().OrderByDescending(x => x.UpdatedAt).Take(100).Select(x => new { x.Id, x.Slug, name = x.DraftName, kind = KindName(x.DraftKind), x.State, x.LatestRevisionNumber, x.UpdatedAt, etag = ETag(x) }).ToListAsync(ct));

    private static async Task<IResult> ListAsync(NovaDbContext db, string? q, int? page, int? pageSize, CancellationToken ct)
    {
        var currentPage = page ?? 1; var size = pageSize ?? 20;
        if (currentPage < 1 || size is < 1 or > 50 || (q?.Length ?? 0) > 100) return Results.Problem(statusCode: 400, title: "Invalid reward pagination or filter.");
        var query = db.RewardDefinitions.AsNoTracking().Where(x => x.State == RewardDefinitionState.Published && x.PublishedRevisionId != null);
        if (!string.IsNullOrWhiteSpace(q)) { var term = q.Trim(); query = query.Where(x => x.DraftName.Contains(term) || x.DraftSummary.Contains(term)); }
        var total = await query.CountAsync(ct); var offset = ((long)currentPage - 1) * size;
        if (offset > int.MaxValue) return Results.Problem(statusCode: 400, title: "Page is out of range.");
        var items = await query.OrderByDescending(x => x.UpdatedAt).Skip((int)offset).Take(size).Select(x => new { id = x.Id, slug = x.Slug, name = x.DraftName, summary = x.DraftSummary, kind = KindName(x.DraftKind), revision = x.LatestRevisionNumber, externalAcknowledgementRequired = true, updatedAt = x.UpdatedAt }).ToListAsync(ct);
        return Results.Ok(new { items, page = currentPage, pageSize = size, total });
    }

    private static async Task<IResult> DetailAsync(NovaDbContext db, string slug, CancellationToken ct)
    {
        var item = await (from definition in db.RewardDefinitions.AsNoTracking()
                          join revision in db.RewardDefinitionRevisions.AsNoTracking() on definition.PublishedRevisionId equals revision.Id
                          where definition.State == RewardDefinitionState.Published && definition.Slug == slug
                          select new { id = definition.Id, revision.Slug, name = revision.Name, summary = revision.Summary, markdown = revision.Markdown, kind = KindName(revision.Kind), revision.Number, revision.DeliveryDescription, externalAcknowledgementRequired = true, revision.PublishedAt, updatedAt = revision.PublishedAt }).SingleOrDefaultAsync(ct);
        return item is null ? Results.NotFound() : Results.Ok(item);
    }

    private static async Task<IResult> CreateAsync(NovaDbContext db, HttpContext http, RewardRequest request, CancellationToken ct)
    {
        if (!TryInput(request, out var input, out var error)) return error!;
        var errors = RewardValidator.Validate(input); if (errors.Count > 0) return Results.ValidationProblem(errors);
        if (await db.RewardDefinitions.AnyAsync(x => x.Slug == input.Slug, ct)) return Conflict("Reward slug already exists.");
        var item = new RewardDefinition { Slug = input.Slug, DraftName = input.Name.Trim(), DraftSummary = input.Summary, DraftMarkdown = input.Markdown, DraftKind = Enum.Parse<RewardDefinitionKind>(input.Kind, true), DraftDeliveryDescription = input.DeliveryDescription.Trim(), ExternalAcknowledgementRequired = true, UpdatedAt = DateTimeOffset.UtcNow };
        db.RewardDefinitions.Add(item); AuditWriter.TryAdd(db, http, "reward.created", "RewardDefinition", item.Id, new { item.Slug });
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException e) when (UniqueViolation(e)) { return Conflict("Reward slug already exists."); }
        return Results.Created($"/api/v1/admin/rewards/{item.Id}", new { item.Id, item.Slug, etag = ETag(item), externalAcknowledgementRequired = true });
    }

    private static async Task<IResult> GetAdminAsync(Guid id, NovaDbContext db, HttpContext http, CancellationToken ct)
    {
        var item = await db.RewardDefinitions.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return Results.NotFound();
        http.Response.Headers.ETag = ETag(item); http.Response.Headers.CacheControl = "no-store";
        return Results.Ok(new { item.Id, item.Slug, name = item.DraftName, summary = item.DraftSummary, markdown = item.DraftMarkdown, kind = KindName(item.DraftKind), deliveryDescription = item.DraftDeliveryDescription, externalAcknowledgementRequired = true, item.State, item.LatestRevisionNumber });
    }

    private static async Task<IResult> UpdateAsync(Guid id, NovaDbContext db, HttpContext http, RewardRequest request, CancellationToken ct)
    {
        var item = await db.RewardDefinitions.SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return Results.NotFound();
        var precondition = CheckPrecondition(http, item); if (precondition is not null) return precondition;
        if (!TryInput(request, out var input, out var error)) return error!;
        var errors = RewardValidator.Validate(input); if (errors.Count > 0) return Results.ValidationProblem(errors);
        if (input.Slug != item.Slug && await db.RewardDefinitions.AnyAsync(x => x.Slug == input.Slug && x.Id != id, ct)) return Conflict("Reward slug already exists.");
        item.Slug = input.Slug; item.DraftName = input.Name.Trim(); item.DraftSummary = input.Summary; item.DraftMarkdown = input.Markdown; item.DraftKind = Enum.Parse<RewardDefinitionKind>(input.Kind, true); item.DraftDeliveryDescription = input.DeliveryDescription.Trim(); item.UpdatedAt = DateTimeOffset.UtcNow;
        AuditWriter.TryAdd(db, http, "reward.edited", "RewardDefinition", item.Id, new { item.Slug });
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { return Stale(); } catch (DbUpdateException e) when (UniqueViolation(e)) { return Conflict("Reward slug already exists."); }
        http.Response.Headers.ETag = ETag(item); return Results.Ok(new { item.Id, etag = ETag(item) });
    }

    private static async Task<IResult> PublishAsync(Guid id, NovaDbContext db, HttpContext http, CancellationToken ct)
    {
        var item = await db.RewardDefinitions.SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return Results.NotFound();
        var precondition = CheckPrecondition(http, item); if (precondition is not null) return precondition;
        if (!Guid.TryParse(http.User.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Unauthorized();
        var revision = RewardDefinitionRevision.FromDraft(item, actorId, DateTimeOffset.UtcNow); db.RewardDefinitionRevisions.Add(revision); item.LatestRevisionNumber = revision.Number; item.PublishedRevisionId = revision.Id; item.State = RewardDefinitionState.Published; item.WasPublished = true; item.UpdatedAt = revision.PublishedAt;
        AuditWriter.TryAdd(db, http, "reward.published", "RewardDefinition", item.Id, new { revision.Number, externalAcknowledgementRequired = true });
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { return Stale(); }
        http.Response.Headers.ETag = ETag(item); return Results.Ok(new { item.Id, revision = revision.Number, externalAcknowledgementRequired = true, etag = ETag(item) });
    }

    private static async Task<IResult> UnpublishAsync(Guid id, NovaDbContext db, HttpContext http, CancellationToken ct)
    {
        var item = await db.RewardDefinitions.SingleOrDefaultAsync(x => x.Id == id, ct); if (item is null) return Results.NotFound();
        var precondition = CheckPrecondition(http, item); if (precondition is not null) return precondition; if (item.State != RewardDefinitionState.Published) return Conflict("Reward is not currently published.");
        item.State = RewardDefinitionState.Unpublished; item.UpdatedAt = DateTimeOffset.UtcNow; AuditWriter.TryAdd(db, http, "reward.unpublished", "RewardDefinition", item.Id);
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { return Stale(); } return Results.NoContent();
    }

    private static bool TryInput(RewardRequest request, out RewardInput input, out IResult? error)
    {
        input = new RewardInput(request.Name ?? "", request.Slug ?? "", request.Summary ?? "", request.Markdown ?? "", request.Kind ?? "", request.DeliveryDescription ?? ""); error = null; return true;
    }
    private static string KindName(RewardDefinitionKind kind) => kind.ToString().ToLowerInvariant();
    private static string ETag(RewardDefinition item) => $"\"{Convert.ToBase64String(item.RowVersion)}\"";
    private static IResult? CheckPrecondition(HttpContext http, RewardDefinition item) => !http.Request.Headers.TryGetValue("If-Match", out var value) || string.IsNullOrWhiteSpace(value.ToString()) ? Results.Problem(statusCode: 428, title: "If-Match is required.") : string.Equals(value.ToString(), ETag(item), StringComparison.Ordinal) ? null : Stale();
    private static IResult Stale() => Results.Problem(statusCode: 412, title: "Reward definition changed; reload before editing.");
    private static IResult Conflict(string title) => Results.Problem(statusCode: 409, title: title);
    private static bool UniqueViolation(DbUpdateException exception) => exception.InnerException is SqlException { Number: 2601 or 2627 };
}

public sealed record RewardRequest(string? Name, string? Slug, string? Summary, string? Markdown, string? Kind, string? DeliveryDescription);
