using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Community;
using NovaHaven.Application.Features.Community.Repositories;
using NovaHaven.Application.Features.Community.Results;
using NovaHaven.Domain.Community;
using NovaHaven.Domain.Community.Entities;
using NovaHaven.Domain.Knowledge;
using NovaHaven.Domain.Knowledge.Entities;
using NovaHaven.Domain.Wiki.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Infrastructure.Persistence.Repositories.Community;

public sealed class EfCommunityRepository(NovaDbContext dbContext) : ICommunityRepository
{
    public Task<int> CountPublishedAsync(CommunityKind? kind, string? search, CancellationToken cancellationToken) =>
        PublishedRevisions(kind, search).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<CommunitySummaryResult>> ListPublishedAsync(
        CommunityKind? kind, string? search, int offset, int pageSize, CancellationToken cancellationToken) =>
        await PublishedRevisions(kind, search).OrderBy(revision => revision.Name)
            .ThenBy(revision => revision.RecordId).Skip(offset).Take(pageSize)
            .Select(revision => new CommunitySummaryResult(revision.RecordId, revision.Slug,
                revision.Name, revision.Summary, revision.Kind, revision.Number, revision.PublishedAt))
            .ToArrayAsync(cancellationToken);

    public async Task<CommunityPublicDetailResult?> FindPublishedAsync(
        CommunityKind kind, string slug, CancellationToken cancellationToken)
    {
        var revision = await PublishedRevisions(kind, null)
            .SingleOrDefaultAsync(item => item.Slug == slug, cancellationToken);
        if (revision is null) return null;

        object? location = revision.LocationRevisionId is Guid locationId
            ? await dbContext.KnowledgeRevisions.AsNoTracking().Where(item => item.Id == locationId)
                .Select(item => new { item.Slug, item.Name, kind = item.Kind.ToString().ToLowerInvariant() })
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        object? season = revision.SeasonRevisionId is Guid seasonId
            ? await dbContext.KnowledgeRevisions.AsNoTracking().Where(item => item.Id == seasonId)
                .Select(item => new { item.Slug, item.Name, kind = item.Kind.ToString().ToLowerInvariant() })
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        var rows = revision.Kind == CommunityKind.Leaderboard
            ? await dbContext.CommunityLeaderboardRevisionRows.AsNoTracking()
                .Where(row => row.RevisionId == revision.Id).OrderBy(row => row.Rank)
                .Select(row => new LeaderboardRowInput(row.Rank, row.ParticipantName, row.Score, row.Note))
                .ToArrayAsync(cancellationToken)
            : [];
        var metadata = new
        {
            startsAt = revision.StartsAt, endsAt = revision.EndsAt, location,
            capacity = revision.Capacity, registrationOpen = revision.RegistrationOpen,
            motto = revision.Motto, discordUrl = revision.DiscordUrl,
            handle = revision.Handle, bio = revision.Bio, avatarUrl = revision.AvatarUrl,
            ownerDisplayName = revision.OwnerDisplayName, galleryMarkdown = revision.GalleryMarkdown,
            leaderboardCategory = revision.LeaderboardCategory, season, rows
        };
        return new CommunityPublicDetailResult(revision.RecordId, revision.Slug, revision.Name,
            revision.Summary, revision.Markdown, revision.Kind, revision.Number, revision.PublishedAt, metadata);
    }

    public async Task<IReadOnlyList<CommunityAdminListItemResult>> ListAdminAsync(
        CommunityKind kind, CancellationToken cancellationToken) =>
        await dbContext.CommunityRecords.AsNoTracking().Where(record => record.Kind == kind)
            .OrderByDescending(record => record.UpdatedAt).Take(100)
            .Select(record => new CommunityAdminListItemResult(record.Id, record.Slug,
                record.DraftName, record.Kind, record.State, record.LatestRevisionNumber,
                record.UpdatedAt, record.RowVersion)).ToArrayAsync(cancellationToken);

    public async Task<CommunityAdminDraftResult?> FindAdminAsync(
        CommunityKind kind, Guid id, CancellationToken cancellationToken)
    {
        var record = await dbContext.CommunityRecords.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.Kind == kind, cancellationToken);
        if (record is null) return null;
        var rows = kind == CommunityKind.Leaderboard
            ? await dbContext.CommunityLeaderboardDraftRows.AsNoTracking().Where(row => row.RecordId == id)
                .OrderBy(row => row.Rank)
                .Select(row => new LeaderboardRowInput(row.Rank, row.ParticipantName, row.Score, row.Note))
                .ToArrayAsync(cancellationToken)
            : [];
        var metadata = new
        {
            startsAt = record.DraftStartsAt, endsAt = record.DraftEndsAt,
            locationEntryId = record.DraftLocationEntryId, capacity = record.DraftCapacity,
            registrationOpen = record.DraftRegistrationOpen, motto = record.DraftMotto,
            discordUrl = record.DraftDiscordUrl, handle = record.DraftHandle, bio = record.DraftBio,
            avatarUrl = record.DraftAvatarUrl, ownerDisplayName = record.DraftOwnerDisplayName,
            galleryMarkdown = record.DraftGalleryMarkdown, leaderboardCategory = record.DraftLeaderboardCategory,
            seasonEntryId = record.DraftSeasonEntryId, rows
        };
        return new CommunityAdminDraftResult(record.Id, record.Slug, record.DraftName,
            record.DraftSummary, record.DraftMarkdown, record.Kind, record.State,
            record.LatestRevisionNumber, metadata, record.RowVersion);
    }

    public Task<CommunityRecord?> FindForUpdateAsync(
        CommunityKind kind, Guid id, CancellationToken cancellationToken) =>
        dbContext.CommunityRecords.SingleOrDefaultAsync(record => record.Id == id && record.Kind == kind, cancellationToken);

    public Task<bool> IsSlugInUseAsync(string slug, Guid? excludingId, CancellationToken cancellationToken)
    {
        var query = dbContext.CommunityRecords.AsNoTracking().Where(record => record.Slug == slug);
        if (excludingId.HasValue) query = query.Where(record => record.Id != excludingId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public async Task<Dictionary<string, string[]>> ValidateDraftReferencesAsync(
        Guid? locationEntryId, Guid? seasonEntryId, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (locationEntryId is Guid location && !await dbContext.KnowledgeEntries.AnyAsync(
                entry => entry.Id == location && entry.Kind == KnowledgeKind.Location, cancellationToken))
            errors["locationEntryId"] = ["Community location must be an existing World Atlas entry."];
        if (seasonEntryId is Guid season && !await dbContext.KnowledgeEntries.AnyAsync(
                entry => entry.Id == season && entry.Kind == KnowledgeKind.Season, cancellationToken))
            errors["seasonEntryId"] = ["Season must be an existing Seasonal Hub entry."];
        return errors;
    }

    public async Task<Dictionary<string, string[]>> ValidatePublicationAsync(
        CommunityRecord record, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (record.DraftLocationEntryId is Guid location)
            await RequirePublishedAsync(location, KnowledgeKind.Location, "locationEntryId", errors, cancellationToken);
        if (record.DraftSeasonEntryId is Guid season)
            await RequirePublishedAsync(season, KnowledgeKind.Season, "seasonEntryId", errors, cancellationToken);
        return errors;
    }

    public Task<Guid?> FindPublishedKnowledgeRevisionIdAsync(
        Guid id, KnowledgeKind kind, CancellationToken cancellationToken) =>
        dbContext.KnowledgeEntries.AsNoTracking().Where(entry => entry.Id == id && entry.Kind == kind
                && entry.State == KnowledgeState.Published && entry.PublishedRevisionId != null)
            .Select(entry => entry.PublishedRevisionId).SingleOrDefaultAsync(cancellationToken);

    public async Task<IReadOnlyList<CommunityRegistrationResult>> ListRegistrationsAsync(
        Guid eventId, CancellationToken cancellationToken) =>
        await dbContext.CommunityEventRegistrations.AsNoTracking().Where(registration => registration.EventRecordId == eventId)
            .OrderByDescending(registration => registration.CreatedAt)
            .Select(registration => new CommunityRegistrationResult(registration.Id, registration.DisplayName,
                registration.Contact, registration.State, registration.CreatedAt, registration.RowVersion))
            .ToArrayAsync(cancellationToken);

    public Task<CommunityEventRegistration?> FindRegistrationForUpdateAsync(
        Guid eventId, Guid registrationId, CancellationToken cancellationToken) =>
        dbContext.CommunityEventRegistrations.SingleOrDefaultAsync(
            registration => registration.Id == registrationId && registration.EventRecordId == eventId,
            cancellationToken);

    public Task<bool> IsEventAsync(Guid eventId, CancellationToken cancellationToken) =>
        dbContext.CommunityRecords.AsNoTracking().AnyAsync(
            record => record.Id == eventId && record.Kind == CommunityKind.Event, cancellationToken);

    public void Add(CommunityRecord record) => dbContext.CommunityRecords.Add(record);

    public void ReplaceLeaderboardDraftRows(Guid recordId, IReadOnlyList<LeaderboardRowInput> rows)
    {
        dbContext.CommunityLeaderboardDraftRows.RemoveRange(
            dbContext.CommunityLeaderboardDraftRows.Where(row => row.RecordId == recordId));
        dbContext.CommunityLeaderboardDraftRows.AddRange(rows.Select(row => new CommunityLeaderboardDraftRow
        {
            RecordId = recordId, Rank = row.Rank, ParticipantName = row.ParticipantName.Trim(),
            Score = row.Score, Note = row.Note.Trim()
        }));
    }

    public void AddRevision(CommunityRecordRevision revision) => dbContext.CommunityRecordRevisions.Add(revision);

    public async Task CopyLeaderboardRowsToRevisionAsync(
        Guid recordId, Guid revisionId, CancellationToken cancellationToken)
    {
        var rows = await dbContext.CommunityLeaderboardDraftRows.AsNoTracking().Where(row => row.RecordId == recordId)
            .OrderBy(row => row.Rank).ToArrayAsync(cancellationToken);
        dbContext.CommunityLeaderboardRevisionRows.AddRange(rows.Select(row => new CommunityLeaderboardRevisionRow
        {
            RevisionId = revisionId, Rank = row.Rank, ParticipantName = row.ParticipantName,
            Score = row.Score, Note = row.Note
        }));
    }

    public void AddRegistration(CommunityEventRegistration registration) => dbContext.CommunityEventRegistrations.Add(registration);

    public void AddAudit(Guid? actorId, string action, Guid entityId, object? details = null)
    {
        if (actorId is null) return;
        var json = JsonSerializer.Serialize(details ?? new { });
        dbContext.AuditEvents.Add(new WikiAuditEvent
        {
            ActorUserId = actorId.Value, Action = action, EntityType = "CommunityRecord",
            EntityId = entityId, DetailsJson = json.Length <= 4000 ? json : json[..4000],
            OccurredAt = DateTimeOffset.UtcNow
        });
    }

    private IQueryable<CommunityRecordRevision> PublishedRevisions(CommunityKind? kind, string? search)
    {
        var query = dbContext.CommunityRecordRevisions.AsNoTracking().Where(revision =>
            dbContext.CommunityRecords.Any(record => record.Id == revision.RecordId
                && record.State == CommunityState.Published && record.PublishedRevisionId == revision.Id));
        if (kind.HasValue) query = query.Where(revision => revision.Kind == kind.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(revision => revision.Name.Contains(search) || revision.Summary.Contains(search));
        return query;
    }

    private async Task RequirePublishedAsync(
        Guid id, KnowledgeKind kind, string key, Dictionary<string, string[]> errors,
        CancellationToken cancellationToken)
    {
        var entry = await dbContext.KnowledgeEntries.AsNoTracking().SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
        if (entry is null || entry.Kind != kind || entry.State != KnowledgeState.Published || entry.PublishedRevisionId is null)
            errors[key] = ["Referenced content must be published before this community record can be published."];
    }
}
