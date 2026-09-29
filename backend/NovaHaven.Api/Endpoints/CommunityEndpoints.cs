using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Api.Infrastructure;
using NovaHaven.Application.Community;
using NovaHaven.Domain.Community;
using NovaHaven.Domain.Community.Entities;
using NovaHaven.Domain.Knowledge;
using NovaHaven.Domain.Knowledge.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Api.Endpoints;

public static class CommunityEndpoints
{
    public static void MapCommunityEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/admin/community").RequireAuthorization("AdminOnly");
        admin.AddEndpointFilter(async (context, next) =>
        {
            if (HttpMethods.IsGet(context.HttpContext.Request.Method)) return await next(context);
            var antiforgery = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
            return await AuthEndpoints.ValidateCsrf(context.HttpContext, antiforgery) ? await next(context) : Results.Problem(statusCode: 400, title: "Invalid anti-forgery token.");
        });
        admin.MapGet("/{kind}", AdminListAsync);
        admin.MapPost("/{kind}", CreateAsync);
        admin.MapGet("/{kind}/{id:guid}", AdminDetailAsync);
        admin.MapPatch("/{kind}/{id:guid}", UpdateAsync);
        admin.MapPost("/{kind}/{id:guid}/publish", PublishAsync);
        admin.MapPost("/{kind}/{id:guid}/unpublish", UnpublishAsync);
        admin.MapGet("/event/{id:guid}/registrations", ListRegistrationsAsync);
        admin.MapPost("/event/{id:guid}/registrations", AddRegistrationAsync);
        admin.MapPatch("/event/{id:guid}/registrations/{registrationId:guid}", UpdateRegistrationAsync);

        var publicGroup = app.MapGroup("/api/v1/community");
        publicGroup.MapGet("", PublicListAsync);
        publicGroup.MapGet("/{kind}", PublicListByKindAsync);
        publicGroup.MapGet("/{kind}/{slug}", PublicDetailAsync);
    }

    private static async Task<IResult> PublicListAsync(NovaDbContext db, string? kind, string? q, int? page, int? pageSize, CancellationToken ct)
    {
        if (!TryParseKind(kind, out var parsed)) return InvalidKind();
        return await PublicListCoreAsync(db, parsed, q, page, pageSize, ct);
    }

    private static async Task<IResult> PublicListByKindAsync(NovaDbContext db, string kind, string? q, int? page, int? pageSize, CancellationToken ct)
    {
        if (!TryParseKind(kind, out var parsed)) return InvalidKind();
        return await PublicListCoreAsync(db, parsed, q, page, pageSize, ct);
    }

    private static async Task<IResult> PublicListCoreAsync(NovaDbContext db, CommunityKind? kind, string? q, int? page, int? pageSize, CancellationToken ct)
    {
        var currentPage = page ?? 1;
        var size = pageSize ?? 20;
        if (currentPage < 1 || size is < 1 or > 50 || (q?.Length ?? 0) > 100) return Results.Problem(statusCode: 400, title: "Invalid community pagination or filter.");
        var query = from record in db.CommunityRecords.AsNoTracking()
                    join revision in db.CommunityRecordRevisions.AsNoTracking() on record.PublishedRevisionId equals (Guid?)revision.Id
                    where record.State == CommunityState.Published
                    select new { Record = record, Revision = revision };
        if (kind.HasValue) query = query.Where(x => x.Revision.Kind == kind.Value);
        if (!string.IsNullOrWhiteSpace(q)) { var term = q.Trim(); query = query.Where(x => x.Revision.Name.Contains(term) || x.Revision.Summary.Contains(term)); }
        var total = await query.CountAsync(ct);
        var offset = ((long)currentPage - 1) * size;
        if (offset > int.MaxValue) return Results.Problem(statusCode: 400, title: "Page is out of range.");
        var rows = await query.OrderBy(x => x.Revision.Name).ThenBy(x => x.Record.Id).Skip((int)offset).Take(size)
            .Select(x => new { id = x.Record.Id, slug = x.Revision.Slug, name = x.Revision.Name, summary = x.Revision.Summary, kind = x.Revision.Kind, revision = x.Revision.Number, publishedAt = x.Revision.PublishedAt }).ToListAsync(ct);
        return Results.Ok(new { items = rows.Select(x => new { x.id, x.slug, x.name, x.summary, kind = KindName(x.kind), x.revision, x.publishedAt }), page = currentPage, pageSize = size, total });
    }

    private static async Task<IResult> PublicDetailAsync(NovaDbContext db, string kind, string slug, CancellationToken ct)
    {
        if (!TryParseKind(kind, out var parsed)) return InvalidKind();
        var row = await (from record in db.CommunityRecords.AsNoTracking()
                         join revision in db.CommunityRecordRevisions.AsNoTracking() on record.PublishedRevisionId equals (Guid?)revision.Id
                         where record.State == CommunityState.Published && revision.Kind == parsed && revision.Slug == slug
                         select new { Record = record, Revision = revision }).SingleOrDefaultAsync(ct);
        if (row is null) return Results.NotFound();
        return Results.Ok(await PublicObjectAsync(db, row.Record, row.Revision, ct));
    }

    private static async Task<IResult> AdminListAsync(NovaDbContext db, string kind, CancellationToken ct)
    {
        if (!TryParseKind(kind, out var parsed)) return InvalidKind();
        var rows = await db.CommunityRecords.AsNoTracking().Where(x => x.Kind == parsed).OrderByDescending(x => x.UpdatedAt).Take(100).ToListAsync(ct);
        return Results.Ok(rows.Select(x => new { x.Id, x.Slug, name = x.DraftName, kind = KindName(x.Kind), state = StateName(x.State), x.LatestRevisionNumber, x.UpdatedAt, etag = ETag(x) }));
    }

    private static async Task<IResult> AdminDetailAsync(NovaDbContext db, HttpContext http, string kind, Guid id, CancellationToken ct)
    {
        if (!TryParseKind(kind, out var parsed)) return InvalidKind();
        var record = await db.CommunityRecords.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id && x.Kind == parsed, ct);
        if (record is null) return Results.NotFound();
        http.Response.Headers.ETag = ETag(record); http.Response.Headers.CacheControl = "no-store";
        return Results.Ok(await DraftObjectAsync(db, record, ct));
    }

    private static async Task<IResult> CreateAsync(NovaDbContext db, HttpContext http, string kind, CommunityRequest request, CancellationToken ct)
    {
        if (!TryBuild(kind, request, out var parsed, out var error)) return error!;
        var references = await ValidateDraftReferencesAsync(db, parsed!, ct);
        if (references.Count > 0) return Results.ValidationProblem(references);
        if (await db.CommunityRecords.AnyAsync(x => x.Slug == parsed!.Common.Slug, ct)) return Conflict("Community slug already exists.");
        var record = new CommunityRecord { Slug = parsed!.Common.Slug.Trim(), DraftName = parsed.Common.Name.Trim(), DraftSummary = parsed.Common.Summary.Trim(), DraftMarkdown = parsed.Common.Markdown, Kind = parsed.Common.Kind, UpdatedAt = DateTimeOffset.UtcNow };
        ApplyDraft(record, parsed); db.CommunityRecords.Add(record); AddDraftRows(db, record, parsed);
        AuditWriter.TryAdd(db, http, "community.created", "CommunityRecord", record.Id, new { record.Kind, record.Slug });
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateException e) when (UniqueViolation(e)) { return Conflict("Community slug already exists."); }
        return Results.Created($"/api/v1/admin/community/{KindName(record.Kind)}/{record.Id}", new { id = record.Id, record.Slug, etag = ETag(record) });
    }

    private static async Task<IResult> UpdateAsync(NovaDbContext db, HttpContext http, string kind, Guid id, CommunityRequest request, CancellationToken ct)
    {
        if (!TryParseKind(kind, out var parsedKind)) return InvalidKind();
        var record = await db.CommunityRecords.SingleOrDefaultAsync(x => x.Id == id && x.Kind == parsedKind, ct);
        if (record is null) return Results.NotFound();
        var precondition = CheckPrecondition(http, record); if (precondition is not null) return precondition;
        if (!TryBuild(kind, request, out var parsed, out var error)) return error!;
        var references = await ValidateDraftReferencesAsync(db, parsed!, ct); if (references.Count > 0) return Results.ValidationProblem(references);
        if (await db.CommunityRecords.AnyAsync(x => x.Slug == parsed!.Common.Slug && x.Id != id, ct)) return Conflict("Community slug already exists.");
        record.Slug = parsed!.Common.Slug.Trim(); record.DraftName = parsed.Common.Name.Trim(); record.DraftSummary = parsed.Common.Summary.Trim(); record.DraftMarkdown = parsed.Common.Markdown; record.UpdatedAt = DateTimeOffset.UtcNow; ApplyDraft(record, parsed);
        db.CommunityLeaderboardDraftRows.RemoveRange(await db.CommunityLeaderboardDraftRows.Where(x => x.RecordId == id).ToListAsync(ct)); AddDraftRows(db, record, parsed);
        AuditWriter.TryAdd(db, http, "community.edited", "CommunityRecord", record.Id, new { record.Kind, record.Slug });
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { return Stale(); } catch (DbUpdateException e) when (UniqueViolation(e)) { return Conflict("Community slug already exists."); }
        http.Response.Headers.ETag = ETag(record); return Results.Ok(new { id = record.Id, etag = ETag(record) });
    }

    private static async Task<IResult> PublishAsync(NovaDbContext db, HttpContext http, string kind, Guid id, CancellationToken ct)
    {
        if (!TryParseKind(kind, out var parsedKind)) return InvalidKind();
        var record = await db.CommunityRecords.SingleOrDefaultAsync(x => x.Id == id && x.Kind == parsedKind, ct); if (record is null) return Results.NotFound();
        var precondition = CheckPrecondition(http, record); if (precondition is not null) return precondition;
        var validation = await ValidatePublicationAsync(db, record, ct); if (validation.Count > 0) return Results.ValidationProblem(validation);
        if (!Guid.TryParse(http.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var actorId)) return Results.Unauthorized();
        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            var now = DateTimeOffset.UtcNow; var revision = CommunityRecordRevision.FromDraft(record, actorId, now);
            revision.LocationRevisionId = record.DraftLocationEntryId is Guid location ? await PublishedKnowledgeRevisionAsync(db, location, KnowledgeKind.Location, ct) : null;
            revision.SeasonRevisionId = record.DraftSeasonEntryId is Guid season ? await PublishedKnowledgeRevisionAsync(db, season, KnowledgeKind.Season, ct) : null;
            db.CommunityRecordRevisions.Add(revision);
            if (record.Kind == CommunityKind.Leaderboard)
            {
                var rows = await db.CommunityLeaderboardDraftRows.AsNoTracking().Where(x => x.RecordId == record.Id).OrderBy(x => x.Rank).ToListAsync(ct);
                db.CommunityLeaderboardRevisionRows.AddRange(rows.Select(x => new CommunityLeaderboardRevisionRow { RevisionId = revision.Id, Rank = x.Rank, ParticipantName = x.ParticipantName, Score = x.Score, Note = x.Note }));
            }
            record.State = CommunityState.Published; record.WasPublished = true; record.PublishedRevisionId = revision.Id; record.LatestRevisionNumber = revision.Number; record.UpdatedAt = now;
            AuditWriter.TryAdd(db, http, "community.published", "CommunityRecord", record.Id, new { record.Kind, revision = revision.Number });
            await db.SaveChangesAsync(ct); await transaction.CommitAsync(ct); http.Response.Headers.ETag = ETag(record);
            return Results.Ok(new { id = record.Id, revisionId = revision.Id, revision = revision.Number, etag = ETag(record) });
        }
        catch (DbUpdateConcurrencyException) { await transaction.RollbackAsync(ct); return Stale(); }
        catch (DbUpdateException e) when (UniqueViolation(e)) { await transaction.RollbackAsync(ct); return Conflict("Community slug or revision already exists."); }
    }

    private static async Task<IResult> UnpublishAsync(NovaDbContext db, HttpContext http, string kind, Guid id, CancellationToken ct)
    {
        if (!TryParseKind(kind, out var parsedKind)) return InvalidKind();
        var record = await db.CommunityRecords.SingleOrDefaultAsync(x => x.Id == id && x.Kind == parsedKind, ct); if (record is null) return Results.NotFound();
        var precondition = CheckPrecondition(http, record); if (precondition is not null) return precondition;
        if (record.State != CommunityState.Published) return Conflict("Community record is not currently published.");
        record.State = CommunityState.Unpublished; record.UpdatedAt = DateTimeOffset.UtcNow; AuditWriter.TryAdd(db, http, "community.unpublished", "CommunityRecord", record.Id, new { record.Kind });
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { return Stale(); }
        http.Response.Headers.ETag = ETag(record); return Results.NoContent();
    }

    private static async Task<IResult> ListRegistrationsAsync(NovaDbContext db, Guid id, CancellationToken ct)
    {
        if (!await db.CommunityRecords.AnyAsync(x => x.Id == id && x.Kind == CommunityKind.Event, ct)) return Results.NotFound();
        var rows = await db.CommunityEventRegistrations.AsNoTracking().Where(x => x.EventRecordId == id).OrderByDescending(x => x.CreatedAt).ToListAsync(ct);
        return Results.Ok(rows.Select(x => new { x.Id, x.DisplayName, x.Contact, state = x.State.ToString().ToLowerInvariant(), x.CreatedAt, etag = RegistrationETag(x) }));
    }

    private static async Task<IResult> AddRegistrationAsync(NovaDbContext db, HttpContext http, Guid id, RegistrationRequest request, CancellationToken ct)
    {
        var eventRecord = await db.CommunityRecords.SingleOrDefaultAsync(x => x.Id == id && x.Kind == CommunityKind.Event, ct); if (eventRecord is null) return Results.NotFound();
        if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Length > 120 || (request.Contact?.Length ?? 0) > 200) return Results.ValidationProblem(new Dictionary<string, string[]> { ["displayName"] = ["Display name is required and contact must be at most 200 characters."] });
        var registration = new CommunityEventRegistration { EventRecordId = id, DisplayName = request.DisplayName.Trim(), Contact = request.Contact?.Trim() ?? "" };
        db.CommunityEventRegistrations.Add(registration); AuditWriter.TryAdd(db, http, "community.registration.created", "CommunityEventRegistration", registration.Id, new { registration.EventRecordId });
        await db.SaveChangesAsync(ct); return Results.Created($"/api/v1/admin/community/event/{id}/registrations", new { id = registration.Id, registration.DisplayName, registration.Contact, state = "pending", etag = RegistrationETag(registration) });
    }

    private static async Task<IResult> UpdateRegistrationAsync(NovaDbContext db, HttpContext http, Guid id, Guid registrationId, RegistrationUpdateRequest request, CancellationToken ct)
    {
        var registration = await db.CommunityEventRegistrations.SingleOrDefaultAsync(x => x.Id == registrationId && x.EventRecordId == id, ct); if (registration is null) return Results.NotFound();
        if (!http.Request.Headers.TryGetValue("If-Match", out var value) || value.Count == 0) return Results.Problem(statusCode: 428, title: "If-Match is required.");
        if (!string.Equals(value.ToString(), RegistrationETag(registration), StringComparison.Ordinal)) return Results.Problem(statusCode: 412, title: "Registration changed; reload before editing.");
        if (!Enum.TryParse<EventRegistrationState>(request.State, true, out var state)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["state"] = ["Unsupported registration state."] });
        registration.State = state; try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { return Results.Problem(statusCode: 412, title: "Registration changed; reload before editing."); }
        AuditWriter.TryAdd(db, http, "community.registration.updated", "CommunityEventRegistration", registration.Id, new { state }); await db.SaveChangesAsync(ct);
        http.Response.Headers.ETag = RegistrationETag(registration); return Results.Ok(new { id = registration.Id, state = state.ToString().ToLowerInvariant(), etag = RegistrationETag(registration) });
    }

    private static bool TryBuild(string routeKind, CommunityRequest request, out ParsedCommunity? parsed, out IResult? error)
    {
        parsed = null; error = null; if (!TryParseKind(routeKind, out var kind)) { error = InvalidKind(); return false; }
        if (!string.IsNullOrWhiteSpace(request.Kind) && (!Enum.TryParse<CommunityKind>(request.Kind, true, out var bodyKind) || bodyKind != kind)) { error = Results.ValidationProblem(new Dictionary<string, string[]> { ["kind"] = ["Kind does not match the route."] }); return false; }
        var common = new CommunityCommonInput(request.Name ?? "", request.Slug ?? "", request.Summary ?? "", request.Markdown ?? "", kind!.Value); var errors = CommunityValidator.ValidateCommon(common);
        CommunityEventInput? @event = null; GuildInput? guild = null; PlayerInput? player = null; HousingInput? housing = null; LeaderboardInput? leaderboard = null;
        switch (kind)
        {
            case CommunityKind.Event: @event = new CommunityEventInput(request.StartsAt, request.EndsAt, request.LocationEntryId, request.Capacity, request.RegistrationOpen); Merge(errors, CommunityValidator.ValidateEvent(@event)); break;
            case CommunityKind.Guild: guild = new GuildInput(request.Motto ?? "", request.DiscordUrl ?? ""); Merge(errors, CommunityValidator.ValidateGuild(guild)); break;
            case CommunityKind.Player: player = new PlayerInput(request.Handle ?? "", request.Bio ?? "", request.AvatarUrl ?? ""); Merge(errors, CommunityValidator.ValidatePlayer(player)); break;
            case CommunityKind.Housing: housing = new HousingInput(request.OwnerDisplayName ?? "", request.GalleryMarkdown ?? "", request.LocationEntryId); Merge(errors, CommunityValidator.ValidateHousing(housing)); break;
            case CommunityKind.Leaderboard: leaderboard = new LeaderboardInput(request.LeaderboardCategory ?? "", (request.Rows ?? []).Select(x => new LeaderboardRowInput(x.Rank, x.ParticipantName ?? "", x.Score, x.Note ?? "")).ToList(), request.SeasonEntryId); Merge(errors, CommunityValidator.ValidateLeaderboard(leaderboard)); break;
        }
        if (errors.Count > 0) { error = Results.ValidationProblem(errors); return false; }
        parsed = new ParsedCommunity(common, @event, guild, player, housing, leaderboard); return true;
    }

    private static async Task<Dictionary<string, string[]>> ValidateDraftReferencesAsync(NovaDbContext db, ParsedCommunity parsed, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        var locationId = parsed.Event?.LocationEntryId ?? parsed.Housing?.LocationEntryId;
        if (locationId is Guid eventLocation && !await db.KnowledgeEntries.AnyAsync(x => x.Id == eventLocation && x.Kind == KnowledgeKind.Location, ct)) errors["locationEntryId"] = ["Community location must be an existing World Atlas entry."];
        if (parsed.Leaderboard?.SeasonEntryId is Guid season && !await db.KnowledgeEntries.AnyAsync(x => x.Id == season && x.Kind == KnowledgeKind.Season, ct)) errors["seasonEntryId"] = ["Season must be an existing Seasonal Hub entry."];
        return errors;
    }

    private static async Task<Dictionary<string, string[]>> ValidatePublicationAsync(NovaDbContext db, CommunityRecord record, CancellationToken ct)
    {
        var errors = new Dictionary<string, string[]>();
        if (record.DraftLocationEntryId is Guid location) await RequirePublishedAsync(db, location, KnowledgeKind.Location, "locationEntryId", errors, ct);
        if (record.DraftSeasonEntryId is Guid season) await RequirePublishedAsync(db, season, KnowledgeKind.Season, "seasonEntryId", errors, ct);
        return errors;
    }

    private static async Task RequirePublishedAsync(NovaDbContext db, Guid id, KnowledgeKind kind, string key, Dictionary<string, string[]> errors, CancellationToken ct)
    {
        var entry = await db.KnowledgeEntries.AsNoTracking().SingleOrDefaultAsync(x => x.Id == id, ct);
        if (entry is null || entry.Kind != kind || entry.State != KnowledgeState.Published || entry.PublishedRevisionId is null) errors[key] = ["Referenced content must be published before this community record can be published."];
    }

    private static void ApplyDraft(CommunityRecord record, ParsedCommunity parsed)
    {
        record.DraftStartsAt = parsed.Event?.StartsAt?.ToUniversalTime(); record.DraftEndsAt = parsed.Event?.EndsAt?.ToUniversalTime(); record.DraftLocationEntryId = parsed.Event?.LocationEntryId ?? parsed.Housing?.LocationEntryId;
        record.DraftCapacity = parsed.Event?.Capacity; record.DraftRegistrationOpen = parsed.Event?.RegistrationOpen ?? false;
        record.DraftMotto = parsed.Guild?.Motto.Trim() ?? ""; record.DraftDiscordUrl = parsed.Guild?.DiscordUrl.Trim() ?? "";
        record.DraftHandle = parsed.Player?.Handle.Trim() ?? ""; record.DraftBio = parsed.Player?.Bio.Trim() ?? ""; record.DraftAvatarUrl = parsed.Player?.AvatarUrl.Trim() ?? "";
        record.DraftOwnerDisplayName = parsed.Housing?.OwnerDisplayName.Trim() ?? ""; record.DraftGalleryMarkdown = parsed.Housing?.GalleryMarkdown ?? "";
        record.DraftLeaderboardCategory = parsed.Leaderboard?.Category.Trim() ?? ""; record.DraftSeasonEntryId = parsed.Leaderboard?.SeasonEntryId;
    }

    private static void AddDraftRows(NovaDbContext db, CommunityRecord record, ParsedCommunity parsed)
    { if (parsed.Leaderboard is not null) db.CommunityLeaderboardDraftRows.AddRange(parsed.Leaderboard.Rows.Select(x => new CommunityLeaderboardDraftRow { RecordId = record.Id, Rank = x.Rank, ParticipantName = x.ParticipantName.Trim(), Score = x.Score, Note = x.Note.Trim() })); }

    private static async Task<object> DraftObjectAsync(NovaDbContext db, CommunityRecord record, CancellationToken ct)
    {
        var rows = record.Kind == CommunityKind.Leaderboard ? await db.CommunityLeaderboardDraftRows.AsNoTracking().Where(x => x.RecordId == record.Id).OrderBy(x => x.Rank).Select(x => new { x.Rank, x.ParticipantName, x.Score, x.Note }).ToListAsync(ct) : [];
        return new { record.Id, record.Slug, name = record.DraftName, summary = record.DraftSummary, markdown = record.DraftMarkdown, kind = KindName(record.Kind), state = StateName(record.State), record.LatestRevisionNumber, metadata = new { startsAt = record.DraftStartsAt, endsAt = record.DraftEndsAt, locationEntryId = record.DraftLocationEntryId, capacity = record.DraftCapacity, registrationOpen = record.DraftRegistrationOpen, motto = record.DraftMotto, discordUrl = record.DraftDiscordUrl, handle = record.DraftHandle, bio = record.DraftBio, avatarUrl = record.DraftAvatarUrl, ownerDisplayName = record.DraftOwnerDisplayName, galleryMarkdown = record.DraftGalleryMarkdown, leaderboardCategory = record.DraftLeaderboardCategory, seasonEntryId = record.DraftSeasonEntryId, rows } };
    }

    private static async Task<object> PublicObjectAsync(NovaDbContext db, CommunityRecord record, CommunityRecordRevision revision, CancellationToken ct)
    {
        var rows = revision.Kind == CommunityKind.Leaderboard ? await db.CommunityLeaderboardRevisionRows.AsNoTracking().Where(x => x.RevisionId == revision.Id).OrderBy(x => x.Rank).Select(x => new { x.Rank, x.ParticipantName, x.Score, x.Note }).ToListAsync(ct) : [];
        var location = revision.LocationRevisionId is Guid locationId ? await db.KnowledgeRevisions.AsNoTracking().Where(x => x.Id == locationId).Select(x => new { x.Slug, x.Name, kind = x.Kind.ToString().ToLowerInvariant() }).SingleOrDefaultAsync(ct) : null;
        var season = revision.SeasonRevisionId is Guid seasonId ? await db.KnowledgeRevisions.AsNoTracking().Where(x => x.Id == seasonId).Select(x => new { x.Slug, x.Name, kind = x.Kind.ToString().ToLowerInvariant() }).SingleOrDefaultAsync(ct) : null;
        return new { id = record.Id, slug = revision.Slug, name = revision.Name, summary = revision.Summary, markdown = revision.Markdown, kind = KindName(revision.Kind), revision = revision.Number, publishedAt = revision.PublishedAt, metadata = new { startsAt = revision.StartsAt, endsAt = revision.EndsAt, location, capacity = revision.Capacity, registrationOpen = revision.RegistrationOpen, motto = revision.Motto, discordUrl = revision.DiscordUrl, handle = revision.Handle, bio = revision.Bio, avatarUrl = revision.AvatarUrl, ownerDisplayName = revision.OwnerDisplayName, galleryMarkdown = revision.GalleryMarkdown, leaderboardCategory = revision.LeaderboardCategory, season, rows } };
    }

    private static async Task<Guid> PublishedKnowledgeRevisionAsync(NovaDbContext db, Guid id, KnowledgeKind kind, CancellationToken ct) => (await db.KnowledgeEntries.AsNoTracking().SingleAsync(x => x.Id == id && x.Kind == kind, ct)).PublishedRevisionId!.Value;

    private static bool TryParseKind(string? value, out CommunityKind? kind)
    { if (string.IsNullOrWhiteSpace(value)) { kind = null; return true; } if (Enum.TryParse<CommunityKind>(value, true, out var parsed) && Enum.IsDefined(parsed)) { kind = parsed; return true; } kind = null; return false; }
    private static void Merge(Dictionary<string, string[]> target, Dictionary<string, string[]> source) { foreach (var pair in source) target[pair.Key] = pair.Value; }
    private static IResult InvalidKind() => Results.Problem(statusCode: 400, title: "Community kind is not supported.");
    private static string KindName(CommunityKind kind) => kind.ToString().ToLowerInvariant();
    private static string StateName(CommunityState state) => state.ToString().ToLowerInvariant();
    private static string ETag(CommunityRecord record) => $"\"{Convert.ToBase64String(record.RowVersion)}\"";
    private static string RegistrationETag(CommunityEventRegistration registration) => $"\"{Convert.ToBase64String(registration.RowVersion)}\"";
    private static IResult? CheckPrecondition(HttpContext http, CommunityRecord record) { if (!http.Request.Headers.TryGetValue("If-Match", out var value) || string.IsNullOrWhiteSpace(value.ToString())) return Results.Problem(statusCode: 428, title: "If-Match is required."); return string.Equals(value.ToString(), ETag(record), StringComparison.Ordinal) ? null : Stale(); }
    private static IResult Stale() => Results.Problem(statusCode: 412, title: "Community record changed; reload before editing.");
    private static IResult Conflict(string title) => Results.Problem(statusCode: 409, title: title);
    private static bool UniqueViolation(DbUpdateException exception) => exception.InnerException is SqlException { Number: 2601 or 2627 };

    private sealed record ParsedCommunity(CommunityCommonInput Common, CommunityEventInput? Event, GuildInput? Guild, PlayerInput? Player, HousingInput? Housing, LeaderboardInput? Leaderboard);
}

public sealed record CommunityRequest(
    string? Name, string? Slug, string? Summary, string? Markdown, string? Kind,
    DateTimeOffset? StartsAt, DateTimeOffset? EndsAt, Guid? LocationEntryId, int? Capacity, bool RegistrationOpen,
    string? Motto, string? DiscordUrl, string? Handle, string? Bio, string? AvatarUrl,
    string? OwnerDisplayName, string? GalleryMarkdown, string? LeaderboardCategory, Guid? SeasonEntryId,
    List<CommunityLeaderboardRowRequest>? Rows);
public sealed record CommunityLeaderboardRowRequest(int Rank, string? ParticipantName, decimal Score, string? Note);
public sealed record RegistrationRequest(string? DisplayName, string? Contact);
public sealed record RegistrationUpdateRequest(string? State);
