namespace NovaHaven.Api.Contracts.Audit;

public sealed record AuditEventResponse(
    Guid Id,
    Guid ActorUserId,
    string Action,
    string EntityType,
    Guid EntityId,
    string DetailsJson,
    DateTimeOffset OccurredAt);
