using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Features.Audit.Queries;
using NovaHaven.Application.Features.Audit.Repositories;
using NovaHaven.Application.Features.Audit.Results;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Infrastructure.Persistence.Repositories.Audit;

public sealed class EfAuditEventRepository(NovaDbContext dbContext) : IAuditEventRepository
{
    public async Task<AuditPageResult> ListAsync(AuditListQuery query, CancellationToken cancellationToken)
    {
        var events = dbContext.AuditEvents.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(query.EntityType))
            events = events.Where(item => item.EntityType == query.EntityType);
        if (query.EntityId is Guid entityId)
            events = events.Where(item => item.EntityId == entityId);

        var total = await events.CountAsync(cancellationToken);
        var offset = (int)(((long)query.Page - 1) * query.PageSize);
        var items = await events
            .OrderByDescending(item => item.OccurredAt)
            .ThenByDescending(item => item.Id)
            .Skip(offset)
            .Take(query.PageSize)
            .Select(item => new AuditEventResult(
                item.Id,
                item.ActorUserId,
                item.Action,
                item.EntityType,
                item.EntityId,
                item.DetailsJson,
                item.OccurredAt))
            .ToArrayAsync(cancellationToken);

        return new AuditPageResult(items, query.Page, query.PageSize, total);
    }
}
