using Microsoft.EntityFrameworkCore;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Api.Endpoints;

public static class AuditEndpoints
{
    public static void MapAuditEndpoints(this WebApplication app)
    {
        app.MapGet("/api/v1/admin/audit", async (NovaDbContext db, string? entityType, Guid? entityId,
            int? page, int? pageSize, CancellationToken ct) =>
        {
            var currentPage = page ?? 1;
            var size = pageSize ?? 50;
            if (currentPage < 1 || size is < 1 or > 100)
                return Results.Problem(statusCode: 400, title: "Invalid audit pagination.");
            var query = db.AuditEvents.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(entityType)) query = query.Where(x => x.EntityType == entityType);
            if (entityId is not null) query = query.Where(x => x.EntityId == entityId.Value);
            var total = await query.CountAsync(ct);
            var offset = ((long)currentPage - 1) * size;
            if (offset > int.MaxValue) return Results.Problem(statusCode: 400, title: "Page is out of range.");
            var items = await query.OrderByDescending(x => x.OccurredAt).ThenByDescending(x => x.Id)
                .Skip((int)offset).Take(size)
                .Select(x => new { x.Id, x.ActorUserId, x.Action, x.EntityType, x.EntityId,
                    x.DetailsJson, x.OccurredAt }).ToListAsync(ct);
            return Results.Ok(new { items, page = currentPage, pageSize = size, total });
        }).RequireAuthorization("AdminOnly");
    }
}
