namespace NovaHaven.Application.Features.Audit.Queries;

public sealed record AuditListQuery(string? EntityType, Guid? EntityId, int Page, int PageSize);
