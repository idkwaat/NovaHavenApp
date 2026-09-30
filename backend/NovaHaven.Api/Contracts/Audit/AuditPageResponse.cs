namespace NovaHaven.Api.Contracts.Audit;

public sealed record AuditPageResponse(
    IReadOnlyList<AuditEventResponse> Items,
    int Page,
    int PageSize,
    int Total);
