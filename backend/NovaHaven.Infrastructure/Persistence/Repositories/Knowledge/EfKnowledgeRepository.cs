using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Features.Knowledge.Repositories;
using NovaHaven.Application.Features.Knowledge.Results;
using NovaHaven.Application.Knowledge;
using NovaHaven.Domain.Catalog;
using NovaHaven.Domain.Catalog.Entities;
using NovaHaven.Domain.Knowledge;
using NovaHaven.Domain.Knowledge.Entities;
using NovaHaven.Domain.Wiki.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Infrastructure.Persistence.Repositories.Knowledge;

public sealed class EfKnowledgeRepository(NovaDbContext dbContext) : IKnowledgeRepository
{
    public Task<int> CountPublishedAsync(
        KnowledgeKind? kind, string? search, CancellationToken cancellationToken) =>
        PublishedRevisions(kind, search).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<KnowledgeSummaryResult>> ListPublishedAsync(
        KnowledgeKind? kind, string? search, int offset, int pageSize, CancellationToken cancellationToken)
    {
        var rows = await PublishedRevisions(kind, search)
            .OrderBy(revision => revision.Name)
            .ThenBy(revision => revision.EntryId)
            .Skip(offset)
            .Take(pageSize)
            .Select(revision => new
            {
                Id = revision.EntryId,
                RevisionId = revision.Id,
                revision.Slug,
                revision.Name,
                revision.Summary,
                revision.Kind,
                revision.Number,
                revision.PublishedAt
            })
            .ToArrayAsync(cancellationToken);

        var revisionIds = rows.Select(row => row.RevisionId).ToArray();
        var previewByRevision = new Dictionary<Guid, string>();
        if (!kind.HasValue || kind == KnowledgeKind.Npc)
        {
            var portraits = await dbContext.NpcProfileRevisions.AsNoTracking()
                .Where(profile => revisionIds.Contains(profile.RevisionId)
                    && profile.PortraitUrl != null && profile.PortraitUrl != "")
                .Select(profile => new { profile.RevisionId, profile.PortraitUrl })
                .ToArrayAsync(cancellationToken);
            foreach (var portrait in portraits)
                previewByRevision.TryAdd(portrait.RevisionId, portrait.PortraitUrl!);
        }
        if (!kind.HasValue || kind == KnowledgeKind.Location)
        {
            var mapImages = await dbContext.WorldLocationRevisions.AsNoTracking()
                .Where(location => revisionIds.Contains(location.RevisionId)
                    && location.MapImageUrl != null && location.MapImageUrl != "")
                .Select(location => new { location.RevisionId, location.MapImageUrl })
                .ToArrayAsync(cancellationToken);
            foreach (var mapImage in mapImages)
                previewByRevision.TryAdd(mapImage.RevisionId, mapImage.MapImageUrl!);
        }

        return rows.Select(row => new KnowledgeSummaryResult(
            row.Id, row.Slug, row.Name, row.Summary, row.Kind, row.Number, row.PublishedAt,
            previewByRevision.GetValueOrDefault(row.RevisionId))).ToArray();
    }

    public async Task<KnowledgePublicDetailResult?> FindPublishedAsync(
        KnowledgeKind kind, string slug, CancellationToken cancellationToken)
    {
        var revision = await PublishedRevisions(kind, null)
            .SingleOrDefaultAsync(item => item.Slug == slug, cancellationToken);
        if (revision is null) return null;

        var metadata = await GetPublishedMetadataAsync(revision.Id, kind, cancellationToken);
        var links = await dbContext.KnowledgeRevisionLinks.AsNoTracking()
            .Where(link => link.RevisionId == revision.Id)
            .OrderBy(link => link.SortOrder)
            .Select(link => new KnowledgePublishedLinkResult(
                link.LinkType, link.TargetSlug, link.TargetName, link.TargetType, link.SortOrder))
            .ToArrayAsync(cancellationToken);

        return new KnowledgePublicDetailResult(
            revision.EntryId, revision.Slug, revision.Name, revision.Summary,
            revision.Markdown, revision.Kind, revision.Number,
            revision.PublishedAt, metadata, links);
    }

    public async Task<IReadOnlyList<KnowledgeAdminListItemResult>> ListAdminAsync(
        KnowledgeKind kind, CancellationToken cancellationToken) =>
        await dbContext.KnowledgeEntries.AsNoTracking()
            .Where(entry => entry.Kind == kind)
            .OrderByDescending(entry => entry.UpdatedAt)
            .Take(100)
            .Select(entry => new KnowledgeAdminListItemResult(
                entry.Id, entry.Slug, entry.DraftName, entry.Kind, entry.State,
                entry.LatestRevisionNumber, entry.UpdatedAt, entry.RowVersion))
            .ToArrayAsync(cancellationToken);

    public async Task<KnowledgeAdminDraftResult?> FindAdminAsync(
        KnowledgeKind kind, Guid id, CancellationToken cancellationToken)
    {
        var entry = await dbContext.KnowledgeEntries.AsNoTracking()
            .SingleOrDefaultAsync(item => item.Id == id && item.Kind == kind, cancellationToken);
        if (entry is null) return null;

        var metadata = await GetDraftMetadataAsync(entry.Id, kind, cancellationToken);
        var links = await dbContext.KnowledgeDraftLinks.AsNoTracking()
            .Where(link => link.EntryId == entry.Id)
            .OrderBy(link => link.SortOrder)
            .Select(link => new KnowledgeDraftLinkResult(
                link.LinkType, link.TargetEntryId, link.TargetCatalogItemId, link.SortOrder))
            .ToArrayAsync(cancellationToken);

        return new KnowledgeAdminDraftResult(
            entry.Id, entry.Slug, entry.DraftName, entry.DraftSummary, entry.DraftMarkdown,
            entry.Kind, entry.State, entry.LatestRevisionNumber, metadata, links, entry.RowVersion);
    }

    public Task<GameKnowledgeEntry?> FindForUpdateAsync(
        KnowledgeKind kind, Guid id, CancellationToken cancellationToken) =>
        dbContext.KnowledgeEntries.SingleOrDefaultAsync(
            entry => entry.Id == id && entry.Kind == kind, cancellationToken);

    public Task<bool> IsSlugInUseAsync(
        string slug, Guid? excludingId, CancellationToken cancellationToken)
    {
        var query = dbContext.KnowledgeEntries.AsNoTracking().Where(entry => entry.Slug == slug);
        if (excludingId.HasValue) query = query.Where(entry => entry.Id != excludingId.Value);
        return query.AnyAsync(cancellationToken);
    }

    public async Task<Dictionary<string, string[]>> ValidateDraftReferencesAsync(
        KnowledgeCommonInput common, NpcInput? npc, QuestInput? quest,
        IReadOnlyList<KnowledgeLinkInput> links, Guid? currentId, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (npc?.LocationEntryId is Guid npcLocation
            && !await dbContext.KnowledgeEntries.AnyAsync(
                entry => entry.Id == npcLocation && entry.Kind == KnowledgeKind.Location, cancellationToken))
            errors["locationEntryId"] = ["Location entry does not exist."];
        if (quest?.GiverNpcEntryId is Guid giver
            && !await dbContext.KnowledgeEntries.AnyAsync(
                entry => entry.Id == giver && entry.Kind == KnowledgeKind.Npc, cancellationToken))
            errors["giverNpcEntryId"] = ["Giver NPC does not exist."];
        if (quest?.LocationEntryId is Guid questLocation
            && !await dbContext.KnowledgeEntries.AnyAsync(
                entry => entry.Id == questLocation && entry.Kind == KnowledgeKind.Location, cancellationToken))
            errors["locationEntryId"] = ["Location entry does not exist."];
        if (currentId.HasValue && links.Any(link => link.TargetEntryId == currentId))
            errors["links"] = ["An entry cannot link to itself."];

        foreach (var link in links)
        {
            if (link.TargetEntryId is Guid target
                && !await dbContext.KnowledgeEntries.AnyAsync(entry => entry.Id == target, cancellationToken))
                errors["links"] = ["A linked knowledge entry does not exist."];
            if (link.TargetCatalogItemId is Guid item
                && !await dbContext.CatalogItems.AnyAsync(catalogItem => catalogItem.Id == item, cancellationToken))
                errors["links"] = ["A linked catalog item does not exist."];
        }
        return errors;
    }

    public async Task<Dictionary<string, string[]>> ValidatePublicationAsync(
        GameKnowledgeEntry entry, CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();
        if (entry.Kind == KnowledgeKind.Npc)
        {
            var npc = await dbContext.NpcProfiles.AsNoTracking()
                .SingleAsync(profile => profile.EntryId == entry.Id, cancellationToken);
            if (npc.LocationEntryId is Guid location)
                await RequirePublishedKindAsync(location, KnowledgeKind.Location, "locationEntryId", errors, cancellationToken);
        }
        if (entry.Kind == KnowledgeKind.Quest)
        {
            var quest = await dbContext.QuestDefinitions.AsNoTracking()
                .SingleAsync(definition => definition.EntryId == entry.Id, cancellationToken);
            if (quest.GiverNpcEntryId is Guid giver)
                await RequirePublishedKindAsync(giver, KnowledgeKind.Npc, "giverNpcEntryId", errors, cancellationToken);
            if (quest.LocationEntryId is Guid location)
                await RequirePublishedKindAsync(location, KnowledgeKind.Location, "locationEntryId", errors, cancellationToken);
        }

        var links = await dbContext.KnowledgeDraftLinks.AsNoTracking()
            .Where(link => link.EntryId == entry.Id).ToArrayAsync(cancellationToken);
        foreach (var link in links)
        {
            if (link.TargetEntryId is Guid target)
                await RequirePublishedKindAsync(target, null, "links", errors, cancellationToken);
            if (link.TargetCatalogItemId is Guid item)
            {
                var published = await dbContext.CatalogItems.AsNoTracking().AnyAsync(
                    catalogItem => catalogItem.Id == item
                        && catalogItem.State == CatalogItemState.Published
                        && catalogItem.PublishedRevisionId != null,
                    cancellationToken);
                if (!published)
                    errors["links"] = ["All linked catalog items must be published before this entry can be published."];
            }
        }
        return errors;
    }

    public void Add(GameKnowledgeEntry entry) => dbContext.KnowledgeEntries.Add(entry);

    public void AddDraftData(
        GameKnowledgeEntry entry, NpcInput? npc, QuestInput? quest, LocationInput? location,
        SeasonInput? season, IReadOnlyList<KnowledgeLinkInput> links)
    {
        if (npc is not null)
            dbContext.NpcProfiles.Add(new NpcProfile
            {
                EntryId = entry.Id,
                Role = npc.Role.Trim(),
                LocationEntryId = npc.LocationEntryId,
                PortraitUrl = npc.PortraitUrl?.Trim()
            });
        if (quest is not null)
        {
            dbContext.QuestDefinitions.Add(new QuestDefinition
            {
                EntryId = entry.Id,
                Difficulty = quest.Difficulty,
                GiverNpcEntryId = quest.GiverNpcEntryId,
                LocationEntryId = quest.LocationEntryId,
                RewardDescription = quest.RewardDescription.Trim()
            });
            dbContext.QuestSteps.AddRange(quest.Steps.Select(step => new QuestStep
            {
                QuestEntryId = entry.Id,
                Position = step.Position,
                Title = step.Title.Trim(),
                Description = step.Description.Trim()
            }));
        }
        if (location is not null)
            dbContext.WorldLocations.Add(new WorldLocation
            {
                EntryId = entry.Id,
                Region = location.Region.Trim(),
                LocationType = location.LocationType.Trim(),
                Latitude = location.Latitude,
                Longitude = location.Longitude,
                MapImageUrl = location.MapImageUrl?.Trim()
            });
        if (season is not null)
            dbContext.SeasonDefinitions.Add(new SeasonDefinition
            {
                EntryId = entry.Id,
                StartsAt = season.StartsAt.ToUniversalTime(),
                EndsAt = season.EndsAt.ToUniversalTime(),
                Theme = season.Theme.Trim(),
                EventDescription = season.EventDescription?.Trim()
            });
        dbContext.KnowledgeDraftLinks.AddRange(links.Select(link => new GameKnowledgeDraftLink
        {
            EntryId = entry.Id,
            LinkType = link.LinkType,
            TargetEntryId = link.TargetEntryId,
            TargetCatalogItemId = link.TargetCatalogItemId,
            SortOrder = link.SortOrder
        }));
    }

    public async Task ReplaceDraftDataAsync(
        Guid id, KnowledgeKind kind, NpcInput? npc, QuestInput? quest, LocationInput? location,
        SeasonInput? season, IReadOnlyList<KnowledgeLinkInput> links, CancellationToken cancellationToken)
    {
        dbContext.KnowledgeDraftLinks.RemoveRange(await dbContext.KnowledgeDraftLinks
            .Where(link => link.EntryId == id).ToArrayAsync(cancellationToken));
        switch (kind)
        {
            case KnowledgeKind.Npc:
            {
                var row = await dbContext.NpcProfiles.FindAsync([id], cancellationToken);
                if (row is not null) dbContext.NpcProfiles.Remove(row);
                break;
            }
            case KnowledgeKind.Quest:
            {
                var row = await dbContext.QuestDefinitions.FindAsync([id], cancellationToken);
                if (row is not null) dbContext.QuestDefinitions.Remove(row);
                dbContext.QuestSteps.RemoveRange(await dbContext.QuestSteps
                    .Where(step => step.QuestEntryId == id).ToArrayAsync(cancellationToken));
                break;
            }
            case KnowledgeKind.Location:
            {
                var row = await dbContext.WorldLocations.FindAsync([id], cancellationToken);
                if (row is not null) dbContext.WorldLocations.Remove(row);
                break;
            }
            case KnowledgeKind.Season:
            {
                var row = await dbContext.SeasonDefinitions.FindAsync([id], cancellationToken);
                if (row is not null) dbContext.SeasonDefinitions.Remove(row);
                break;
            }
        }

        AddDraftData(new GameKnowledgeEntry { Id = id }, npc, quest, location, season, links);
    }

    public async Task AddRevisionDataAsync(
        GameKnowledgeEntry entry, GameKnowledgeRevision revision, CancellationToken cancellationToken)
    {
        switch (entry.Kind)
        {
            case KnowledgeKind.Npc:
            {
                var draft = await dbContext.NpcProfiles.SingleAsync(profile => profile.EntryId == entry.Id, cancellationToken);
                Guid? locationRevisionId = draft.LocationEntryId is Guid locationId
                    ? await PublishedRevisionIdAsync(locationId, KnowledgeKind.Location, cancellationToken)
                    : null;
                dbContext.NpcProfileRevisions.Add(new NpcProfileRevision
                {
                    RevisionId = revision.Id, Role = draft.Role,
                    LocationRevisionId = locationRevisionId, PortraitUrl = draft.PortraitUrl
                });
                break;
            }
            case KnowledgeKind.Quest:
            {
                var draft = await dbContext.QuestDefinitions.SingleAsync(definition => definition.EntryId == entry.Id, cancellationToken);
                Guid? giverRevisionId = draft.GiverNpcEntryId is Guid giverId
                    ? await PublishedRevisionIdAsync(giverId, KnowledgeKind.Npc, cancellationToken)
                    : null;
                Guid? locationRevisionId = draft.LocationEntryId is Guid locationId
                    ? await PublishedRevisionIdAsync(locationId, KnowledgeKind.Location, cancellationToken)
                    : null;
                dbContext.QuestDefinitionRevisions.Add(new QuestDefinitionRevision
                {
                    RevisionId = revision.Id, Difficulty = draft.Difficulty,
                    GiverNpcRevisionId = giverRevisionId, LocationRevisionId = locationRevisionId,
                    RewardDescription = draft.RewardDescription
                });
                var steps = await dbContext.QuestSteps.AsNoTracking()
                    .Where(step => step.QuestEntryId == entry.Id).OrderBy(step => step.Position)
                    .ToArrayAsync(cancellationToken);
                dbContext.QuestStepRevisions.AddRange(steps.Select(step => new QuestStepRevision
                {
                    QuestRevisionId = revision.Id, Position = step.Position,
                    Title = step.Title, Description = step.Description
                }));
                break;
            }
            case KnowledgeKind.Location:
            {
                var draft = await dbContext.WorldLocations.SingleAsync(location => location.EntryId == entry.Id, cancellationToken);
                dbContext.WorldLocationRevisions.Add(new WorldLocationRevision
                {
                    RevisionId = revision.Id, Region = draft.Region, LocationType = draft.LocationType,
                    Latitude = draft.Latitude, Longitude = draft.Longitude, MapImageUrl = draft.MapImageUrl
                });
                break;
            }
            case KnowledgeKind.Season:
            {
                var draft = await dbContext.SeasonDefinitions.SingleAsync(season => season.EntryId == entry.Id, cancellationToken);
                dbContext.SeasonDefinitionRevisions.Add(new SeasonDefinitionRevision
                {
                    RevisionId = revision.Id, StartsAt = draft.StartsAt, EndsAt = draft.EndsAt,
                    Theme = draft.Theme, EventDescription = draft.EventDescription
                });
                break;
            }
        }

        dbContext.KnowledgeRevisions.Add(revision);
        var links = await dbContext.KnowledgeDraftLinks.AsNoTracking()
            .Where(link => link.EntryId == entry.Id).OrderBy(link => link.SortOrder)
            .ToArrayAsync(cancellationToken);
        foreach (var link in links)
        {
            if (link.TargetEntryId is Guid targetId)
            {
                var target = await dbContext.KnowledgeEntries.AsNoTracking()
                    .SingleAsync(candidate => candidate.Id == targetId, cancellationToken);
                var targetRevisionId = target.PublishedRevisionId!.Value;
                var targetRevision = await dbContext.KnowledgeRevisions.AsNoTracking()
                    .SingleAsync(candidate => candidate.Id == targetRevisionId, cancellationToken);
                dbContext.KnowledgeRevisionLinks.Add(new GameKnowledgeRevisionLink
                {
                    RevisionId = revision.Id, LinkType = link.LinkType, TargetRevisionId = targetRevisionId,
                    TargetSlug = targetRevision.Slug, TargetName = targetRevision.Name,
                    TargetType = KindName(targetRevision.Kind), SortOrder = link.SortOrder
                });
            }
            else if (link.TargetCatalogItemId is Guid catalogItemId)
            {
                var item = await dbContext.CatalogItems.AsNoTracking()
                    .SingleAsync(candidate => candidate.Id == catalogItemId, cancellationToken);
                var itemRevision = await dbContext.CatalogItemRevisions.AsNoTracking()
                    .SingleAsync(candidate => candidate.Id == item.PublishedRevisionId!.Value, cancellationToken);
                dbContext.KnowledgeRevisionLinks.Add(new GameKnowledgeRevisionLink
                {
                    RevisionId = revision.Id, LinkType = link.LinkType,
                    TargetCatalogItemRevisionId = itemRevision.Id,
                    TargetSlug = itemRevision.Slug, TargetName = itemRevision.Name,
                    TargetType = "catalog", SortOrder = link.SortOrder
                });
            }
        }
    }

    public void AddAudit(Guid? actorId, string action, Guid entryId, object? details = null)
    {
        if (actorId is null) return;
        var json = JsonSerializer.Serialize(details ?? new { });
        dbContext.AuditEvents.Add(new WikiAuditEvent
        {
            ActorUserId = actorId.Value,
            Action = action,
            EntityType = "GameKnowledgeEntry",
            EntityId = entryId,
            DetailsJson = json.Length <= 4000 ? json : json[..4000],
            OccurredAt = DateTimeOffset.UtcNow
        });
    }

    private IQueryable<GameKnowledgeRevision> PublishedRevisions(
        KnowledgeKind? kind, string? search)
    {
        var query = dbContext.KnowledgeRevisions.AsNoTracking()
            .Where(revision => dbContext.KnowledgeEntries.Any(entry =>
                entry.Id == revision.EntryId
                && entry.State == KnowledgeState.Published
                && entry.PublishedRevisionId == revision.Id));
        if (kind.HasValue) query = query.Where(revision => revision.Kind == kind.Value);
        if (!string.IsNullOrWhiteSpace(search))
            query = query.Where(revision => revision.Name.Contains(search) || revision.Summary.Contains(search));
        return query;
    }

    private async Task<object?> GetDraftMetadataAsync(
        Guid entryId, KnowledgeKind kind, CancellationToken cancellationToken) => kind switch
    {
        KnowledgeKind.Npc => await dbContext.NpcProfiles.AsNoTracking()
            .Where(profile => profile.EntryId == entryId)
            .Select(profile => new { role = profile.Role, locationEntryId = profile.LocationEntryId, portraitUrl = profile.PortraitUrl })
            .SingleOrDefaultAsync(cancellationToken),
        KnowledgeKind.Quest => await GetQuestDraftMetadataAsync(entryId, cancellationToken),
        KnowledgeKind.Location => await dbContext.WorldLocations.AsNoTracking()
            .Where(location => location.EntryId == entryId)
            .Select(location => new { region = location.Region, locationType = location.LocationType, latitude = location.Latitude, longitude = location.Longitude, mapImageUrl = location.MapImageUrl })
            .SingleOrDefaultAsync(cancellationToken),
        KnowledgeKind.Season => await dbContext.SeasonDefinitions.AsNoTracking()
            .Where(season => season.EntryId == entryId)
            .Select(season => new { startsAt = season.StartsAt, endsAt = season.EndsAt, theme = season.Theme, eventDescription = season.EventDescription })
            .SingleOrDefaultAsync(cancellationToken),
        _ => null
    };

    private async Task<object> GetQuestDraftMetadataAsync(Guid entryId, CancellationToken cancellationToken)
    {
        var quest = await dbContext.QuestDefinitions.AsNoTracking()
            .SingleAsync(definition => definition.EntryId == entryId, cancellationToken);
        var steps = await dbContext.QuestSteps.AsNoTracking()
            .Where(step => step.QuestEntryId == entryId).OrderBy(step => step.Position)
            .Select(step => new { step.Position, step.Title, step.Description }).ToArrayAsync(cancellationToken);
        return new
        {
            difficulty = quest.Difficulty,
            giverNpcEntryId = quest.GiverNpcEntryId,
            locationEntryId = quest.LocationEntryId,
            rewardDescription = quest.RewardDescription,
            steps
        };
    }

    private async Task<object?> GetPublishedMetadataAsync(
        Guid revisionId, KnowledgeKind kind, CancellationToken cancellationToken) => kind switch
    {
        KnowledgeKind.Npc => await GetPublishedNpcMetadataAsync(revisionId, cancellationToken),
        KnowledgeKind.Quest => await GetPublishedQuestMetadataAsync(revisionId, cancellationToken),
        KnowledgeKind.Location => await dbContext.WorldLocationRevisions.AsNoTracking()
            .Where(location => location.RevisionId == revisionId)
            .Select(location => new { region = location.Region, locationType = location.LocationType, latitude = location.Latitude, longitude = location.Longitude, mapImageUrl = location.MapImageUrl })
            .SingleOrDefaultAsync(cancellationToken),
        KnowledgeKind.Season => await dbContext.SeasonDefinitionRevisions.AsNoTracking()
            .Where(season => season.RevisionId == revisionId)
            .Select(season => new { startsAt = season.StartsAt, endsAt = season.EndsAt, theme = season.Theme, eventDescription = season.EventDescription })
            .SingleOrDefaultAsync(cancellationToken),
        _ => null
    };

    private async Task<object> GetPublishedNpcMetadataAsync(Guid revisionId, CancellationToken cancellationToken)
    {
        var npc = await dbContext.NpcProfileRevisions.AsNoTracking()
            .SingleAsync(profile => profile.RevisionId == revisionId, cancellationToken);
        var location = npc.LocationRevisionId is Guid locationId
            ? await dbContext.KnowledgeRevisions.AsNoTracking()
                .Where(revision => revision.Id == locationId)
                .Select(revision => new { revision.Slug, revision.Name, kind = KindName(revision.Kind) })
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        return new { role = npc.Role, portraitUrl = npc.PortraitUrl, location };
    }

    private async Task<object> GetPublishedQuestMetadataAsync(Guid revisionId, CancellationToken cancellationToken)
    {
        var quest = await dbContext.QuestDefinitionRevisions.AsNoTracking()
            .SingleAsync(definition => definition.RevisionId == revisionId, cancellationToken);
        var giver = quest.GiverNpcRevisionId is Guid giverId
            ? await dbContext.KnowledgeRevisions.AsNoTracking()
                .Where(revision => revision.Id == giverId)
                .Select(revision => new { revision.Slug, revision.Name, kind = KindName(revision.Kind) })
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        var location = quest.LocationRevisionId is Guid locationId
            ? await dbContext.KnowledgeRevisions.AsNoTracking()
                .Where(revision => revision.Id == locationId)
                .Select(revision => new { revision.Slug, revision.Name, kind = KindName(revision.Kind) })
                .SingleOrDefaultAsync(cancellationToken)
            : null;
        var steps = await dbContext.QuestStepRevisions.AsNoTracking()
            .Where(step => step.QuestRevisionId == revisionId).OrderBy(step => step.Position)
            .Select(step => new { step.Position, step.Title, step.Description }).ToArrayAsync(cancellationToken);
        return new
        {
            difficulty = quest.Difficulty,
            giver,
            location,
            rewardDescription = quest.RewardDescription,
            steps
        };
    }

    private async Task<Guid> PublishedRevisionIdAsync(
        Guid entryId, KnowledgeKind expectedKind, CancellationToken cancellationToken) =>
        (await dbContext.KnowledgeEntries.AsNoTracking().SingleAsync(
            entry => entry.Id == entryId && entry.Kind == expectedKind, cancellationToken))
            .PublishedRevisionId!.Value;

    private async Task RequirePublishedKindAsync(
        Guid id, KnowledgeKind? expectedKind, string errorKey,
        Dictionary<string, string[]> errors, CancellationToken cancellationToken)
    {
        var entry = await dbContext.KnowledgeEntries.AsNoTracking()
            .SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (entry is null || entry.State != KnowledgeState.Published || entry.PublishedRevisionId is null
            || expectedKind.HasValue && entry.Kind != expectedKind.Value)
            errors[errorKey] = ["Referenced knowledge must exist and be published before this entry can be published."];
    }

    private static string KindName(KnowledgeKind kind) => kind.ToString().ToLowerInvariant();
}
