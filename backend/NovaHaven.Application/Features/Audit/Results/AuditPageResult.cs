namespace NovaHaven.Application.Features.Audit.Results;

public sealed record AuditEventResult(
    Guid Id,
    Guid ActorUserId,
    string Action,
    string EntityType,
    Guid EntityId,
    string DetailsJson,
    DateTimeOffset OccurredAt);

public sealed record AuditPageResult(
    IReadOnlyList<AuditEventResult> Items,
    int Page,
    int PageSize,
    int Total);
