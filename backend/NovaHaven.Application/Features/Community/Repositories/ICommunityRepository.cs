using NovaHaven.Application.Community;
using NovaHaven.Application.Features.Community.Results;
using NovaHaven.Domain.Community;
using NovaHaven.Domain.Community.Entities;
using NovaHaven.Domain.Knowledge;

namespace NovaHaven.Application.Features.Community.Repositories;

public interface ICommunityRepository
{
    Task<int> CountPublishedAsync(CommunityKind? kind, string? search, CancellationToken cancellationToken);
    Task<IReadOnlyList<CommunitySummaryResult>> ListPublishedAsync(
        CommunityKind? kind, string? search, int offset, int pageSize, CancellationToken cancellationToken);
    Task<CommunityPublicDetailResult?> FindPublishedAsync(
        CommunityKind kind, string slug, CancellationToken cancellationToken);
    Task<IReadOnlyList<CommunityAdminListItemResult>> ListAdminAsync(
        CommunityKind kind, CancellationToken cancellationToken);
    Task<CommunityAdminDraftResult?> FindAdminAsync(CommunityKind kind, Guid id, CancellationToken cancellationToken);
    Task<CommunityRecord?> FindForUpdateAsync(CommunityKind kind, Guid id, CancellationToken cancellationToken);
    Task<bool> IsSlugInUseAsync(string slug, Guid? excludingId, CancellationToken cancellationToken);
    Task<Dictionary<string, string[]>> ValidateDraftReferencesAsync(
        Guid? locationEntryId, Guid? seasonEntryId, CancellationToken cancellationToken);
    Task<Dictionary<string, string[]>> ValidatePublicationAsync(
        CommunityRecord record, CancellationToken cancellationToken);
    Task<Guid?> FindPublishedKnowledgeRevisionIdAsync(Guid id, KnowledgeKind kind, CancellationToken cancellationToken);
    Task<IReadOnlyList<CommunityRegistrationResult>> ListRegistrationsAsync(Guid eventId, CancellationToken cancellationToken);
    Task<CommunityEventRegistration?> FindRegistrationForUpdateAsync(
        Guid eventId, Guid registrationId, CancellationToken cancellationToken);
    Task<bool> IsEventAsync(Guid eventId, CancellationToken cancellationToken);
    void Add(CommunityRecord record);
    void ReplaceLeaderboardDraftRows(Guid recordId, IReadOnlyList<LeaderboardRowInput> rows);
    void AddRevision(CommunityRecordRevision revision);
    Task CopyLeaderboardRowsToRevisionAsync(Guid recordId, Guid revisionId, CancellationToken cancellationToken);
    void AddRegistration(CommunityEventRegistration registration);
    void AddAudit(Guid? actorId, string action, Guid entityId, object? details = null);
}
