namespace NovaHaven.Application.Features.Operations.Results;

public sealed record OperationsDiagnosticsResult(
    DateTimeOffset GeneratedAt,
    DatabaseDiagnosticsResult Database,
    ContentDiagnosticsResult Content,
    IReadOnlyList<IntegrationDiagnosticsResult> Integrations);

public sealed record DatabaseDiagnosticsResult(
    string Provider,
    bool CanConnect,
    int AppliedMigrations,
    int PendingMigrations,
    IReadOnlyList<string> PendingMigrationNames);

public sealed record ContentDiagnosticsResult(
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

public sealed record IntegrationDiagnosticsResult(
    string CapabilityKey,
    string Status,
    DateTimeOffset UpdatedAt);

public sealed record OperationsDataResult(
    DatabaseDiagnosticsResult Database,
    ContentDiagnosticsResult Content,
    IReadOnlyList<IntegrationDiagnosticsResult> Integrations);
