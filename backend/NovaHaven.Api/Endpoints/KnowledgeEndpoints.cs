using System.Security.Claims;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Api.Infrastructure;
using NovaHaven.Application.Knowledge;
using NovaHaven.Domain.Catalog;
using NovaHaven.Domain.Catalog.Entities;
using NovaHaven.Domain.Knowledge;
using NovaHaven.Domain.Knowledge.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Api.Endpoints;

public static class KnowledgeEndpoints
{
    public static void MapKnowledgeEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/admin/knowledge").RequireAuthorization("AdminOnly");
        admin.AddEndpointFilter(async (context, next) =>
        {
            if (HttpMethods.IsGet(context.HttpContext.Request.Method)) return await next(context);
            var antiforgery = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
            return await AuthEndpoints.ValidateCsrf(context.HttpContext, antiforgery)
                ? await next(context)
                : Results.Problem(statusCode: 400, title: "Invalid anti-forgery token.");
        });
        admin.MapGet("/{kind}", AdminListAsync);
        admin.MapPost("/{kind}", CreateAsync);
        admin.MapGet("/{kind}/{id:guid}", AdminDetailAsync);
        admin.MapPatch("/{kind}/{id:guid}", UpdateAsync);
        admin.MapPost("/{kind}/{id:guid}/publish", PublishAsync);
        admin.MapPost("/{kind}/{id:guid}/unpublish", UnpublishAsync);

        var publicGroup = app.MapGroup("/api/v1/knowledge");
        publicGroup.MapGet("", PublicListAsync);
        publicGroup.MapGet("/{kind}", PublicListByKindAsync);
        publicGroup.MapGet("/{kind}/{slug}", PublicDetailAsync);
    }

    private static async Task<IResult> PublicListAsync(
        NovaDbContext db, string? kind, string? q, int? page, int? pageSize, CancellationToken ct)
    {
        if (!TryParseKind(kind, out var parsedKind)) return InvalidKind();
        return await PublicListCoreAsync(db, parsedKind, q, page, pageSize, ct);
    }

    private static async Task<IResult> PublicListByKindAsync(
        NovaDbContext db, string kind, string? q, int? page, int? pageSize, CancellationToken ct)
    {
        if (!TryParseKind(kind, out var parsedKind)) return InvalidKind();
        return await PublicListCoreAsync(db, parsedKind, q, page, pageSize, ct);
    }

    private static async Task<IResult> PublicListCoreAsync(
        NovaDbContext db, KnowledgeKind? kind, string? q, int? page, int? pageSize, CancellationToken ct)
    {
        var currentPage = page ?? 1;
        var size = pageSize ?? 20;
        if (currentPage < 1 || size is < 1 or > 50 || (q?.Length ?? 0) > 100)
            return Results.Problem(statusCode: 400, title: "Invalid knowledge pagination or filter.");

        var query = from entry in db.KnowledgeEntries.AsNoTracking()
                    join revision in db.KnowledgeRevisions.AsNoTracking()
                        on entry.PublishedRevisionId equals (Guid?)revision.Id
                    where entry.State == KnowledgeState.Published
                    select new { Entry = entry, Revision = revision };
        if (kind.HasValue) query = query.Where(x => x.Revision.Kind == kind.Value);
        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.Trim();
            query = query.Where(x => x.Revision.Name.Contains(term) || x.Revision.Summary.Contains(term));
        }

        var total = await query.CountAsync(ct);
        var offset = ((long)currentPage - 1) * size;
        if (offset > int.MaxValue) return Results.Problem(statusCode: 400, title: "Page is out of range.");
        var rows = await query.OrderBy(x => x.Revision.Name).ThenBy(x => x.Entry.Id)
            .Skip((int)offset).Take(size)
            .Select(x => new
            {
                id = x.Entry.Id,
                revisionId = x.Revision.Id,
                slug = x.Revision.Slug,
                name = x.Revision.Name,
                summary = x.Revision.Summary,
                kind = x.Revision.Kind,
                revision = x.Revision.Number,
                publishedAt = x.Revision.PublishedAt
            }).ToListAsync(ct);
        var revisionIds = rows.Select(x => x.revisionId).ToArray();
        var previewByRevision = new Dictionary<Guid, string>();
        if (!kind.HasValue || kind == KnowledgeKind.Npc)
        {
            var npcImages = await db.NpcProfileRevisions.AsNoTracking()
                .Where(x => revisionIds.Contains(x.RevisionId) && x.PortraitUrl != null && x.PortraitUrl != "")
                .Select(x => new { x.RevisionId, x.PortraitUrl })
                .ToListAsync(ct);
            foreach (var image in npcImages) previewByRevision.TryAdd(image.RevisionId, image.PortraitUrl!);
        }
        if (!kind.HasValue || kind == KnowledgeKind.Location)
        {
            var mapImages = await db.WorldLocationRevisions.AsNoTracking()
                .Where(x => revisionIds.Contains(x.RevisionId) && x.MapImageUrl != null && x.MapImageUrl != "")
                .Select(x => new { x.RevisionId, x.MapImageUrl })
                .ToListAsync(ct);
            foreach (var image in mapImages) previewByRevision.TryAdd(image.RevisionId, image.MapImageUrl!);
        }
        return Results.Ok(new
        {
            items = rows.Select(x => new
            {
                x.id, x.slug, x.name, x.summary, kind = KindName(x.kind), x.revision, x.publishedAt,
                previewImageUrl = previewByRevision.GetValueOrDefault(x.revisionId)
            }),
            page = currentPage,
            pageSize = size,
            total
        });
    }

    private static async Task<IResult> PublicDetailAsync(NovaDbContext db, string kind, string slug, CancellationToken ct)
    {
        if (!TryParseKind(kind, out var parsedKind)) return InvalidKind();
        var row = await (from entry in db.KnowledgeEntries.AsNoTracking()
                         join revision in db.KnowledgeRevisions.AsNoTracking()
                             on entry.PublishedRevisionId equals (Guid?)revision.Id
                         where entry.State == KnowledgeState.Published && revision.Kind == parsedKind && revision.Slug == slug
                         select new { Entry = entry, Revision = revision }).SingleOrDefaultAsync(ct);
        if (row is null) return Results.NotFound();
        return Results.Ok(await PublicDetailObjectAsync(db, row.Entry, row.Revision, ct));
    }

    private static async Task<IResult> AdminListAsync(NovaDbContext db, string kind, CancellationToken ct)
    {
        if (!TryParseKind(kind, out var parsedKind)) return InvalidKind();
        var rows = await db.KnowledgeEntries.AsNoTracking()
            .Where(x => x.Kind == parsedKind)
            .OrderByDescending(x => x.UpdatedAt).Take(100).ToListAsync(ct);
        return Results.Ok(rows.Select(x => new
        {
            x.Id, x.Slug, name = x.DraftName, kind = KindName(x.Kind), state = StateName(x.State),
            x.LatestRevisionNumber, x.UpdatedAt, etag = ETag(x)
        }));
    }

    private static async Task<IResult> AdminDetailAsync(NovaDbContext db, HttpContext http, string kind, Guid id, CancellationToken ct)
    {
        if (!TryParseKind(kind, out var parsedKind)) return InvalidKind();
        var entry = await db.KnowledgeEntries.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.Kind == parsedKind, ct);
        if (entry is null) return Results.NotFound();
        http.Response.Headers.ETag = ETag(entry);
        http.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await DraftObjectAsync(db, entry, ct));
    }

    private static async Task<IResult> CreateAsync(
        NovaDbContext db, HttpContext http, string kind, KnowledgeRequest request, CancellationToken ct)
    {
        if (!TryBuild(kind, request, out var parsed, out var error)) return error!;
        var references = await ValidateDraftReferencesAsync(db, parsed!, null, ct);
        if (references.Count > 0) return Results.ValidationProblem(references);
        if (await db.KnowledgeEntries.AnyAsync(x => x.Slug == parsed!.Common.Slug, ct))
            return Conflict("Knowledge slug already exists.");

        var entry = new GameKnowledgeEntry
        {
            Slug = parsed!.Common.Slug.Trim(),
            DraftName = parsed.Common.Name.Trim(),
            DraftSummary = parsed.Common.Summary?.Trim() ?? "",
            DraftMarkdown = parsed.Common.Markdown,
            Kind = parsed.Common.Kind,
            UpdatedAt = DateTimeOffset.UtcNow
        };
        db.KnowledgeEntries.Add(entry);
        AddDraftData(db, entry, parsed);
        AuditWriter.TryAdd(db, http, "knowledge.created", "GameKnowledgeEntry", entry.Id, new { entry.Kind, entry.Slug });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateException e) when (UniqueViolation(e)) { return Conflict("Knowledge slug already exists."); }
        return Results.Created($"/api/v1/admin/knowledge/{KindName(entry.Kind)}/{entry.Id}", new { id = entry.Id, entry.Slug, etag = ETag(entry) });
    }

    private static async Task<IResult> UpdateAsync(
        NovaDbContext db, HttpContext http, string kind, Guid id, KnowledgeRequest request, CancellationToken ct)
    {
        if (!TryParseKind(kind, out var parsedKind)) return InvalidKind();
        var entry = await db.KnowledgeEntries.SingleOrDefaultAsync(x => x.Id == id && x.Kind == parsedKind, ct);
        if (entry is null) return Results.NotFound();
        var precondition = CheckPrecondition(http, entry);
        if (precondition is not null) return precondition;
        if (!TryBuild(kind, request, out var parsed, out var error)) return error!;
        var references = await ValidateDraftReferencesAsync(db, parsed!, id, ct);
        if (references.Count > 0) return Results.ValidationProblem(references);
        if (await db.KnowledgeEntries.AnyAsync(x => x.Slug == parsed!.Common.Slug && x.Id != id, ct))
            return Conflict("Knowledge slug already exists.");

        entry.Slug = parsed!.Common.Slug.Trim();
        entry.DraftName = parsed.Common.Name.Trim();
        entry.DraftSummary = parsed.Common.Summary?.Trim() ?? "";
        entry.DraftMarkdown = parsed.Common.Markdown;
        entry.UpdatedAt = DateTimeOffset.UtcNow;
        await RemoveDraftDataAsync(db, entry.Id, parsedKind!.Value, ct);
        AddDraftData(db, entry, parsed);
        AuditWriter.TryAdd(db, http, "knowledge.edited", "GameKnowledgeEntry", entry.Id, new { entry.Kind, entry.Slug });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Stale(); }
        catch (DbUpdateException e) when (UniqueViolation(e)) { return Conflict("Knowledge slug already exists."); }
        http.Response.Headers.ETag = ETag(entry);
        return Results.Ok(new { id = entry.Id, etag = ETag(entry) });
    }

    private static async Task<IResult> PublishAsync(NovaDbContext db, HttpContext http, string kind, Guid id, CancellationToken ct)
    {
        if (!TryParseKind(kind, out var parsedKind)) return InvalidKind();
        var entry = await db.KnowledgeEntries.SingleOrDefaultAsync(x => x.Id == id && x.Kind == parsedKind, ct);
        if (entry is null) return Results.NotFound();
        var precondition = CheckPrecondition(http, entry);
        if (precondition is not null) return precondition;
        var validation = await ValidatePublicationAsync(db, entry, ct);
        if (validation.Count > 0) return Results.ValidationProblem(validation);
        var actor = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(actor, out var actorId)) return Results.Unauthorized();

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var now = DateTimeOffset.UtcNow;
            var revision = GameKnowledgeRevision.FromDraft(entry, actorId, now);
            db.KnowledgeRevisions.Add(revision);
            await AddRevisionDataAsync(db, entry, revision, ct);
            entry.State = KnowledgeState.Published;
            entry.WasPublished = true;
            entry.PublishedRevisionId = revision.Id;
            entry.LatestRevisionNumber = revision.Number;
            entry.UpdatedAt = now;
            AuditWriter.TryAdd(db, http, "knowledge.published", "GameKnowledgeEntry", entry.Id, new { entry.Kind, revision = revision.Number });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            http.Response.Headers.ETag = ETag(entry);
            return Results.Ok(new { id = entry.Id, revisionId = revision.Id, revision = revision.Number, etag = ETag(entry) });
        }
        catch (DbUpdateConcurrencyException) { await transaction.RollbackAsync(ct); return Stale(); }
        catch (DbUpdateException e) when (UniqueViolation(e)) { await transaction.RollbackAsync(ct); return Conflict("Knowledge slug or revision already exists."); }
    }

    private static async Task<IResult> UnpublishAsync(NovaDbContext db, HttpContext http, string kind, Guid id, CancellationToken ct)
    {
        if (!TryParseKind(kind, out var parsedKind)) return InvalidKind();
        var entry = await db.KnowledgeEntries.SingleOrDefaultAsync(x => x.Id == id && x.Kind == parsedKind, ct);
        if (entry is null) return Results.NotFound();
        var precondition = CheckPrecondition(http, entry);
        if (precondition is not null) return precondition;
        if (entry.State != KnowledgeState.Published) return Conflict("Knowledge entry is not currently published.");
        entry.State = KnowledgeState.Unpublished;
        entry.UpdatedAt = DateTimeOffset.UtcNow;
        AuditWriter.TryAdd(db, http, "knowledge.unpublished", "GameKnowledgeEntry", entry.Id, new { entry.Kind });
        try { await db.SaveChangesAsync(ct); }
        catch (DbUpdateConcurrencyException) { return Stale(); }
        http.Response.Headers.ETag = ETag(entry);
        return Results.NoContent();
    }

    private static bool TryBuild(string routeKind, KnowledgeRequest request, out ParsedKnowledge? parsed, out IResult? error)
    {
        parsed = null;
        error = null;
        if (!TryParseKind(routeKind, out var kind)) { error = InvalidKind(); return false; }
        if (!string.IsNullOrWhiteSpace(request.Kind) && (!Enum.TryParse<KnowledgeKind>(request.Kind, true, out var bodyKind) || bodyKind != kind))
        {
            error = Results.ValidationProblem(new Dictionary<string, string[]> { ["kind"] = ["Kind does not match the route."] });
            return false;
        }
        var common = new KnowledgeCommonInput(request.Name ?? "", request.Slug ?? "", request.Summary ?? "", request.Markdown ?? "", kind!.Value);
        var errors = KnowledgeValidator.ValidateCommon(common);
        NpcInput? npc = null;
        QuestInput? quest = null;
        LocationInput? location = null;
        SeasonInput? season = null;
        switch (kind)
        {
            case KnowledgeKind.Npc:
                npc = new NpcInput(request.Role ?? "", request.LocationEntryId, request.PortraitUrl);
                Merge(errors, KnowledgeValidator.ValidateNpc(npc));
                break;
            case KnowledgeKind.Quest:
                quest = new QuestInput(request.Difficulty ?? 0, request.GiverNpcEntryId, request.LocationEntryId,
                    request.RewardDescription ?? "", (request.Steps ?? []).Select(x => new QuestStepInput(x.Position, x.Title ?? "", x.Description ?? "")).ToList());
                Merge(errors, KnowledgeValidator.ValidateQuest(quest));
                break;
            case KnowledgeKind.Location:
                location = new LocationInput(request.Region ?? "", request.LocationType ?? "", request.Latitude, request.Longitude, request.MapImageUrl);
                Merge(errors, KnowledgeValidator.ValidateLocation(location));
                break;
            case KnowledgeKind.Season:
                season = new SeasonInput(request.StartsAt ?? default, request.EndsAt ?? default, request.Theme ?? "", request.EventDescription);
                Merge(errors, KnowledgeValidator.ValidateSeason(season));
                break;
        }
        var links = (request.Links ?? []).Select(x => new KnowledgeLinkInput(x.LinkType, x.TargetEntryId, x.TargetCatalogItemId, x.SortOrder)).ToList();
        Merge(errors, KnowledgeValidator.ValidateLinks(links));
        if (links.Any(x => x.TargetEntryId == null && x.TargetCatalogItemId == null)) errors["links"] = ["Every link must have a target."];
        if (errors.Count > 0) { error = Results.ValidationProblem(errors); return false; }
        parsed = new ParsedKnowledge(common, npc, quest, location, season, links);
        return true;
    }

    private static async Task<Dictionary<string, string[]>> ValidateDraftReferencesAsync(NovaDbContext db, ParsedKnowledge parsed, Guid? currentId, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (parsed.Npc?.LocationEntryId is Guid npcLocation && !await db.KnowledgeEntries.AnyAsync(x => x.Id == npcLocation && x.Kind == KnowledgeKind.Location, ct))
            errors["locationEntryId"] = ["Location entry does not exist."];
        if (parsed.Quest?.GiverNpcEntryId is Guid giver && !await db.KnowledgeEntries.AnyAsync(x => x.Id == giver && x.Kind == KnowledgeKind.Npc, ct))
            errors["giverNpcEntryId"] = ["Giver NPC does not exist."];
        if (parsed.Quest?.LocationEntryId is Guid questLocation && !await db.KnowledgeEntries.AnyAsync(x => x.Id == questLocation && x.Kind == KnowledgeKind.Location, ct))
            errors["locationEntryId"] = ["Location entry does not exist."];
        if (currentId.HasValue && parsed.Links.Any(x => x.TargetEntryId == currentId)) errors["links"] = ["An entry cannot link to itself."];
        foreach (var link in parsed.Links)
        {
            if (link.TargetEntryId is Guid target && !await db.KnowledgeEntries.AnyAsync(x => x.Id == target, ct)) errors["links"] = ["A linked knowledge entry does not exist."];
            if (link.TargetCatalogItemId is Guid item && !await db.CatalogItems.AnyAsync(x => x.Id == item, ct)) errors["links"] = ["A linked catalog item does not exist."];
        }
        return errors;
    }

    private static async Task<Dictionary<string, string[]>> ValidatePublicationAsync(NovaDbContext db, GameKnowledgeEntry entry, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (entry.Kind == KnowledgeKind.Npc)
        {
            var npc = await db.NpcProfiles.AsNoTracking().SingleAsync(x => x.EntryId == entry.Id, ct);
            if (npc.LocationEntryId is Guid location) await RequirePublishedKindAsync(db, location, KnowledgeKind.Location, "locationEntryId", errors, ct);
        }
        if (entry.Kind == KnowledgeKind.Quest)
        {
            var quest = await db.QuestDefinitions.AsNoTracking().SingleAsync(x => x.EntryId == entry.Id, ct);
            if (quest.GiverNpcEntryId is Guid giver) await RequirePublishedKindAsync(db, giver, KnowledgeKind.Npc, "giverNpcEntryId", errors, ct);
            if (quest.LocationEntryId is Guid location) await RequirePublishedKindAsync(db, location, KnowledgeKind.Location, "locationEntryId", errors, ct);
        }
        var links = await db.KnowledgeDraftLinks.AsNoTracking().Where(x => x.EntryId == entry.Id).ToListAsync(ct);
        foreach (var link in links)
        {
            if (link.TargetEntryId is Guid target) await RequirePublishedKindAsync(db, target, null, "links", errors, ct);
            if (link.TargetCatalogItemId is Guid item)
            {
                var published = await db.CatalogItems.AnyAsync(x => x.Id == item && x.State == CatalogItemState.Published && x.PublishedRevisionId != null, ct);
                if (!published) errors["links"] = ["All linked catalog items must be published before this entry can be published."];
            }
        }
        return errors;
    }

    private static async Task RequirePublishedKindAsync(NovaDbContext db, Guid id, KnowledgeKind? expected, string key, Dictionary<string, string[]> errors, CancellationToken ct)
    {
        var target = await db.KnowledgeEntries.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (target is null || target.State != KnowledgeState.Published || target.PublishedRevisionId is null || (expected.HasValue && target.Kind != expected.Value))
            errors[key] = ["Referenced knowledge must exist and be published before this entry can be published."];
    }

    private static void AddDraftData(NovaDbContext db, GameKnowledgeEntry entry, ParsedKnowledge parsed)
    {
        if (parsed.Npc is not null) db.NpcProfiles.Add(new NpcProfile { EntryId = entry.Id, Role = parsed.Npc.Role.Trim(), LocationEntryId = parsed.Npc.LocationEntryId, PortraitUrl = parsed.Npc.PortraitUrl?.Trim() });
        if (parsed.Quest is not null)
        {
            db.QuestDefinitions.Add(new QuestDefinition { EntryId = entry.Id, Difficulty = parsed.Quest.Difficulty, GiverNpcEntryId = parsed.Quest.GiverNpcEntryId, LocationEntryId = parsed.Quest.LocationEntryId, RewardDescription = parsed.Quest.RewardDescription.Trim() });
            db.QuestSteps.AddRange(parsed.Quest.Steps.Select(x => new QuestStep { QuestEntryId = entry.Id, Position = x.Position, Title = x.Title.Trim(), Description = x.Description.Trim() }));
        }
        if (parsed.Location is not null) db.WorldLocations.Add(new WorldLocation { EntryId = entry.Id, Region = parsed.Location.Region.Trim(), LocationType = parsed.Location.LocationType.Trim(), Latitude = parsed.Location.Latitude, Longitude = parsed.Location.Longitude, MapImageUrl = parsed.Location.MapImageUrl?.Trim() });
        if (parsed.Season is not null) db.SeasonDefinitions.Add(new SeasonDefinition { EntryId = entry.Id, StartsAt = parsed.Season.StartsAt.ToUniversalTime(), EndsAt = parsed.Season.EndsAt.ToUniversalTime(), Theme = parsed.Season.Theme.Trim(), EventDescription = parsed.Season.EventDescription?.Trim() });
        db.KnowledgeDraftLinks.AddRange(parsed.Links.Select(x => new GameKnowledgeDraftLink { EntryId = entry.Id, LinkType = x.LinkType, TargetEntryId = x.TargetEntryId, TargetCatalogItemId = x.TargetCatalogItemId, SortOrder = x.SortOrder }));
    }

    private static async Task RemoveDraftDataAsync(NovaDbContext db, Guid id, KnowledgeKind kind, CancellationToken ct)
    {
        var links = await db.KnowledgeDraftLinks.Where(x => x.EntryId == id).ToListAsync(ct); db.KnowledgeDraftLinks.RemoveRange(links);
        if (kind == KnowledgeKind.Npc) { var row = await db.NpcProfiles.FindAsync([id], ct); if (row is not null) db.NpcProfiles.Remove(row); }
        if (kind == KnowledgeKind.Quest)
        {
            var row = await db.QuestDefinitions.FindAsync([id], ct); if (row is not null) db.QuestDefinitions.Remove(row);
            db.QuestSteps.RemoveRange(await db.QuestSteps.Where(x => x.QuestEntryId == id).ToListAsync(ct));
        }
        if (kind == KnowledgeKind.Location) { var row = await db.WorldLocations.FindAsync([id], ct); if (row is not null) db.WorldLocations.Remove(row); }
        if (kind == KnowledgeKind.Season) { var row = await db.SeasonDefinitions.FindAsync([id], ct); if (row is not null) db.SeasonDefinitions.Remove(row); }
    }

    private static async Task AddRevisionDataAsync(NovaDbContext db, GameKnowledgeEntry entry, GameKnowledgeRevision revision, CancellationToken ct)
    {
        switch (entry.Kind)
        {
            case KnowledgeKind.Npc:
            {
                var draft = await db.NpcProfiles.SingleAsync(x => x.EntryId == entry.Id, ct);
                Guid? locationRevision = draft.LocationEntryId is Guid location ? await PublishedRevisionIdAsync(db, location, KnowledgeKind.Location, ct) : null;
                db.NpcProfileRevisions.Add(new NpcProfileRevision { RevisionId = revision.Id, Role = draft.Role, LocationRevisionId = locationRevision, PortraitUrl = draft.PortraitUrl });
                break;
            }
            case KnowledgeKind.Quest:
            {
                var draft = await db.QuestDefinitions.SingleAsync(x => x.EntryId == entry.Id, ct);
                Guid? giverRevision = draft.GiverNpcEntryId is Guid giver ? await PublishedRevisionIdAsync(db, giver, KnowledgeKind.Npc, ct) : null;
                Guid? locationRevision = draft.LocationEntryId is Guid location ? await PublishedRevisionIdAsync(db, location, KnowledgeKind.Location, ct) : null;
                db.QuestDefinitionRevisions.Add(new QuestDefinitionRevision { RevisionId = revision.Id, Difficulty = draft.Difficulty, GiverNpcRevisionId = giverRevision, LocationRevisionId = locationRevision, RewardDescription = draft.RewardDescription });
                var steps = await db.QuestSteps.Where(x => x.QuestEntryId == entry.Id).OrderBy(x => x.Position).ToListAsync(ct);
                db.QuestStepRevisions.AddRange(steps.Select(x => new QuestStepRevision { QuestRevisionId = revision.Id, Position = x.Position, Title = x.Title, Description = x.Description }));
                break;
            }
            case KnowledgeKind.Location:
            {
                var draft = await db.WorldLocations.SingleAsync(x => x.EntryId == entry.Id, ct);
                db.WorldLocationRevisions.Add(new WorldLocationRevision { RevisionId = revision.Id, Region = draft.Region, LocationType = draft.LocationType, Latitude = draft.Latitude, Longitude = draft.Longitude, MapImageUrl = draft.MapImageUrl });
                break;
            }
            case KnowledgeKind.Season:
            {
                var draft = await db.SeasonDefinitions.SingleAsync(x => x.EntryId == entry.Id, ct);
                db.SeasonDefinitionRevisions.Add(new SeasonDefinitionRevision { RevisionId = revision.Id, StartsAt = draft.StartsAt, EndsAt = draft.EndsAt, Theme = draft.Theme, EventDescription = draft.EventDescription });
                break;
            }
        }
        var links = await db.KnowledgeDraftLinks.AsNoTracking().Where(x => x.EntryId == entry.Id).OrderBy(x => x.SortOrder).ToListAsync(ct);
        foreach (var link in links)
        {
            if (link.TargetEntryId is Guid targetId)
            {
                var target = await db.KnowledgeEntries.AsNoTracking().SingleAsync(x => x.Id == targetId, ct);
                var targetRevisionId = target.PublishedRevisionId!.Value;
                var targetRevision = await db.KnowledgeRevisions.AsNoTracking().SingleAsync(x => x.Id == targetRevisionId, ct);
                db.KnowledgeRevisionLinks.Add(new GameKnowledgeRevisionLink { RevisionId = revision.Id, LinkType = link.LinkType, TargetRevisionId = targetRevisionId, TargetSlug = targetRevision.Slug, TargetName = targetRevision.Name, TargetType = KindName(targetRevision.Kind), SortOrder = link.SortOrder });
            }
            else if (link.TargetCatalogItemId is Guid itemId)
            {
                var item = await db.CatalogItems.AsNoTracking().SingleAsync(x => x.Id == itemId, ct);
                var itemRevision = await db.CatalogItemRevisions.AsNoTracking().SingleAsync(x => x.Id == item.PublishedRevisionId!.Value, ct);
                db.KnowledgeRevisionLinks.Add(new GameKnowledgeRevisionLink { RevisionId = revision.Id, LinkType = link.LinkType, TargetCatalogItemRevisionId = itemRevision.Id, TargetSlug = itemRevision.Slug, TargetName = itemRevision.Name, TargetType = "catalog", SortOrder = link.SortOrder });
            }
        }
    }

    private static async Task<Guid> PublishedRevisionIdAsync(NovaDbContext db, Guid id, KnowledgeKind expected, CancellationToken ct)
    {
        return (await db.KnowledgeEntries.AsNoTracking().SingleAsync(x => x.Id == id && x.Kind == expected, ct)).PublishedRevisionId!.Value;
    }

    private static async Task<object> DraftObjectAsync(NovaDbContext db, GameKnowledgeEntry entry, CancellationToken ct)
    {
        object? metadata = entry.Kind switch
        {
            KnowledgeKind.Npc => await db.NpcProfiles.AsNoTracking().Where(x => x.EntryId == entry.Id).Select(x => new { role = x.Role, locationEntryId = x.LocationEntryId, portraitUrl = x.PortraitUrl }).SingleOrDefaultAsync(ct),
            KnowledgeKind.Quest => await QuestDraftMetadataAsync(db, entry.Id, ct),
            KnowledgeKind.Location => await db.WorldLocations.AsNoTracking().Where(x => x.EntryId == entry.Id).Select(x => new { region = x.Region, locationType = x.LocationType, latitude = x.Latitude, longitude = x.Longitude, mapImageUrl = x.MapImageUrl }).SingleOrDefaultAsync(ct),
            KnowledgeKind.Season => await db.SeasonDefinitions.AsNoTracking().Where(x => x.EntryId == entry.Id).Select(x => new { startsAt = x.StartsAt, endsAt = x.EndsAt, theme = x.Theme, eventDescription = x.EventDescription }).SingleOrDefaultAsync(ct),
            _ => null
        };
        var links = await db.KnowledgeDraftLinks.AsNoTracking().Where(x => x.EntryId == entry.Id).OrderBy(x => x.SortOrder).Select(x => new { linkType = LinkName(x.LinkType), x.TargetEntryId, x.TargetCatalogItemId, x.SortOrder }).ToListAsync(ct);
        return new { entry.Id, entry.Slug, name = entry.DraftName, summary = entry.DraftSummary, markdown = entry.DraftMarkdown, kind = KindName(entry.Kind), state = StateName(entry.State), entry.LatestRevisionNumber, metadata, links };
    }

    private static async Task<object> PublicDetailObjectAsync(NovaDbContext db, GameKnowledgeEntry entry, GameKnowledgeRevision revision, CancellationToken ct)
    {
        object? metadata = revision.Kind switch
        {
            KnowledgeKind.Npc => await PublicNpcMetadataAsync(db, revision.Id, ct),
            KnowledgeKind.Quest => await PublicQuestMetadataAsync(db, revision.Id, ct),
            KnowledgeKind.Location => await db.WorldLocationRevisions.AsNoTracking().Where(x => x.RevisionId == revision.Id).Select(x => new { region = x.Region, locationType = x.LocationType, latitude = x.Latitude, longitude = x.Longitude, mapImageUrl = x.MapImageUrl }).SingleOrDefaultAsync(ct),
            KnowledgeKind.Season => await db.SeasonDefinitionRevisions.AsNoTracking().Where(x => x.RevisionId == revision.Id).Select(x => new { startsAt = x.StartsAt, endsAt = x.EndsAt, theme = x.Theme, eventDescription = x.EventDescription }).SingleOrDefaultAsync(ct),
            _ => null
        };
        var links = await db.KnowledgeRevisionLinks.AsNoTracking().Where(x => x.RevisionId == revision.Id).OrderBy(x => x.SortOrder).Select(x => new { linkType = LinkName(x.LinkType), slug = x.TargetSlug, name = x.TargetName, type = x.TargetType, x.SortOrder }).ToListAsync(ct);
        return new { id = entry.Id, slug = revision.Slug, name = revision.Name, summary = revision.Summary, markdown = revision.Markdown, kind = KindName(revision.Kind), revision = revision.Number, publishedAt = revision.PublishedAt, metadata, links };
    }

    private static async Task<object> QuestDraftMetadataAsync(NovaDbContext db, Guid id, CancellationToken ct)
    {
        var row = await db.QuestDefinitions.AsNoTracking().SingleAsync(x => x.EntryId == id, ct);
        var steps = await db.QuestSteps.AsNoTracking().Where(x => x.QuestEntryId == id).OrderBy(x => x.Position).Select(x => new { x.Position, x.Title, x.Description }).ToListAsync(ct);
        return new { difficulty = row.Difficulty, giverNpcEntryId = row.GiverNpcEntryId, locationEntryId = row.LocationEntryId, rewardDescription = row.RewardDescription, steps };
    }

    private static async Task<object> PublicNpcMetadataAsync(NovaDbContext db, Guid id, CancellationToken ct)
    {
        var row = await db.NpcProfileRevisions.AsNoTracking().SingleAsync(x => x.RevisionId == id, ct);
        var location = row.LocationRevisionId is Guid locationId ? await db.KnowledgeRevisions.AsNoTracking().Where(x => x.Id == locationId).Select(x => new { x.Slug, x.Name, kind = KindName(x.Kind) }).SingleOrDefaultAsync(ct) : null;
        return new { role = row.Role, portraitUrl = row.PortraitUrl, location };
    }

    private static async Task<object> PublicQuestMetadataAsync(NovaDbContext db, Guid id, CancellationToken ct)
    {
        var row = await db.QuestDefinitionRevisions.AsNoTracking().SingleAsync(x => x.RevisionId == id, ct);
        var giver = row.GiverNpcRevisionId is Guid giverId ? await db.KnowledgeRevisions.AsNoTracking().Where(x => x.Id == giverId).Select(x => new { x.Slug, x.Name, kind = KindName(x.Kind) }).SingleOrDefaultAsync(ct) : null;
        var location = row.LocationRevisionId is Guid locationId ? await db.KnowledgeRevisions.AsNoTracking().Where(x => x.Id == locationId).Select(x => new { x.Slug, x.Name, kind = KindName(x.Kind) }).SingleOrDefaultAsync(ct) : null;
        var steps = await db.QuestStepRevisions.AsNoTracking().Where(x => x.QuestRevisionId == id).OrderBy(x => x.Position).Select(x => new { x.Position, x.Title, x.Description }).ToListAsync(ct);
        return new { difficulty = row.Difficulty, giver, location, rewardDescription = row.RewardDescription, steps };
    }

    private static void Merge(Dictionary<string, string[]> target, Dictionary<string, string[]> source)
    { foreach (var pair in source) target[pair.Key] = pair.Value; }

    private static bool TryParseKind(string? value, out KnowledgeKind? kind)
    {
        if (string.IsNullOrWhiteSpace(value)) { kind = null; return true; }
        if (Enum.TryParse<KnowledgeKind>(value, true, out var parsed) && Enum.IsDefined(parsed)) { kind = parsed; return true; }
        kind = null; return false;
    }

    private static IResult InvalidKind() => Results.Problem(statusCode: 400, title: "Knowledge kind is not supported.");
    private static string KindName(KnowledgeKind kind) => kind.ToString().ToLowerInvariant();
    private static string StateName(KnowledgeState state) => state.ToString().ToLowerInvariant();
    private static string LinkName(KnowledgeLinkType type) => type.ToString().ToLowerInvariant();
    private static string ETag(GameKnowledgeEntry entry) => $"\"{Convert.ToBase64String(entry.RowVersion)}\"";
    private static IResult? CheckPrecondition(HttpContext http, GameKnowledgeEntry entry)
    {
        if (!http.Request.Headers.TryGetValue("If-Match", out var value) || string.IsNullOrWhiteSpace(value.ToString())) return Results.Problem(statusCode: 428, title: "If-Match is required.");
        return string.Equals(value.ToString(), ETag(entry), StringComparison.Ordinal) ? null : Stale();
    }
    private static IResult Stale() => Results.Problem(statusCode: 412, title: "Knowledge entry changed; reload before editing.");
    private static IResult Conflict(string title) => Results.Problem(statusCode: 409, title: title);
    private static bool UniqueViolation(DbUpdateException exception) => exception.InnerException is SqlException { Number: 2601 or 2627 };

    private sealed record ParsedKnowledge(KnowledgeCommonInput Common, NpcInput? Npc, QuestInput? Quest, LocationInput? Location, SeasonInput? Season, IReadOnlyList<KnowledgeLinkInput> Links);
}

public sealed record KnowledgeRequest(
    string? Name, string? Slug, string? Summary, string? Markdown, string? Kind,
    string? Role, Guid? LocationEntryId, string? PortraitUrl,
    int? Difficulty, Guid? GiverNpcEntryId, string? RewardDescription, List<KnowledgeStepRequest>? Steps,
    string? Region, string? LocationType, decimal? Latitude, decimal? Longitude, string? MapImageUrl,
    DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, string? Theme, string? EventDescription,
    List<KnowledgeLinkRequest>? Links);

public sealed record KnowledgeStepRequest(int Position, string? Title, string? Description);
public sealed record KnowledgeLinkRequest(KnowledgeLinkType LinkType, Guid? TargetEntryId, Guid? TargetCatalogItemId, int SortOrder);
