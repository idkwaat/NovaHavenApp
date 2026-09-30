using NovaHaven.Application.Features.Knowledge.Results;
using NovaHaven.Application.Knowledge;
using NovaHaven.Domain.Knowledge;
using NovaHaven.Domain.Knowledge.Entities;

namespace NovaHaven.Application.Features.Knowledge.Repositories;

public interface IKnowledgeRepository
{
    Task<int> CountPublishedAsync(KnowledgeKind? kind, string? search, CancellationToken cancellationToken);

    Task<IReadOnlyList<KnowledgeSummaryResult>> ListPublishedAsync(
        KnowledgeKind? kind, string? search, int offset, int pageSize, CancellationToken cancellationToken);

    Task<KnowledgePublicDetailResult?> FindPublishedAsync(
        KnowledgeKind kind, string slug, CancellationToken cancellationToken);

    Task<IReadOnlyList<KnowledgeAdminListItemResult>> ListAdminAsync(
        KnowledgeKind kind, CancellationToken cancellationToken);

    Task<KnowledgeAdminDraftResult?> FindAdminAsync(
        KnowledgeKind kind, Guid id, CancellationToken cancellationToken);

    Task<GameKnowledgeEntry?> FindForUpdateAsync(
        KnowledgeKind kind, Guid id, CancellationToken cancellationToken);

    Task<bool> IsSlugInUseAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);

    Task<Dictionary<string, string[]>> ValidateDraftReferencesAsync(
        KnowledgeCommonInput common,
        NpcInput? npc,
        QuestInput? quest,
        IReadOnlyList<KnowledgeLinkInput> links,
        Guid? currentId,
        CancellationToken cancellationToken);

    Task<Dictionary<string, string[]>> ValidatePublicationAsync(
        GameKnowledgeEntry entry, CancellationToken cancellationToken);

    void Add(GameKnowledgeEntry entry);

    void AddDraftData(
        GameKnowledgeEntry entry,
        NpcInput? npc,
        QuestInput? quest,
        LocationInput? location,
        SeasonInput? season,
        IReadOnlyList<KnowledgeLinkInput> links);

    Task ReplaceDraftDataAsync(
        Guid id,
        KnowledgeKind kind,
        NpcInput? npc,
        QuestInput? quest,
        LocationInput? location,
        SeasonInput? season,
        IReadOnlyList<KnowledgeLinkInput> links,
        CancellationToken cancellationToken);

    Task AddRevisionDataAsync(
        GameKnowledgeEntry entry,
        GameKnowledgeRevision revision,
        CancellationToken cancellationToken);

    void AddAudit(Guid? actorId, string action, Guid entryId, object? details = null);
}
