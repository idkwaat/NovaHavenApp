namespace NovaHaven.Api.Contracts.Operations;

public sealed record OperationsDiagnosticsResponse(
    DateTimeOffset GeneratedAt,
    DatabaseDiagnosticsResponse Database,
    ContentDiagnosticsResponse Content,
    IReadOnlyList<IntegrationDiagnosticsResponse> Integrations);

public sealed record DatabaseDiagnosticsResponse(
    string Provider,
    bool CanConnect,
    int AppliedMigrations,
    int PendingMigrations,
    IReadOnlyList<string> PendingMigrationNames);

public sealed record ContentDiagnosticsResponse(
    int WikiArticles,
    int PublishedWikiArticles,
    int KnowledgeEntries,
    int PublishedKnowledgeEntries,
    int CommunityRecords,
    int PublishedCommunityRecords,
    int CatalogItems,
    int PublishedCatalogItems,
    int Rewards,
    int PublishedRewards,
    int CommerceOffers,
    int PublishedCommerceOffers,
    int AuditEvents);

public sealed record IntegrationDiagnosticsResponse(
    string CapabilityKey,
    string Status,
    DateTimeOffset UpdatedAt);
