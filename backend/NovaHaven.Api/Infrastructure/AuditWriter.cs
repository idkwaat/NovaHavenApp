using System.Security.Claims;
using System.Text.Json;
using NovaHaven.Domain.Wiki;
using NovaHaven.Domain.Wiki.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Api.Infrastructure;

public static class AuditWriter
{
    public static bool TryAdd(NovaDbContext db, HttpContext http, string action, string entityType,
        Guid entityId, object? details = null)
    {
        var actor = http.User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!Guid.TryParse(actor, out var actorId)) return false;
        var detailsJson = JsonSerializer.Serialize(details ?? new { });
        db.AuditEvents.Add(new WikiAuditEvent
        {
            ActorUserId = actorId,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            DetailsJson = detailsJson.Length <= 4000 ? detailsJson : detailsJson[..4000],
            OccurredAt = DateTimeOffset.UtcNow
        });
        return true;
    }
}
