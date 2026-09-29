using Microsoft.EntityFrameworkCore;
using NovaHaven.Api.Infrastructure;
using NovaHaven.Application.Integration;
using NovaHaven.Domain.Integration;
using NovaHaven.Domain.Integration.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Api.Endpoints;

public static class IntegrationEndpoints
{
    private const string UnavailableMessage = "No external adapter or provider contract is configured in this local workspace.";

    private static readonly (string Key, string Name, string Owner)[] Known =
    [
        ("minecraft-bridge", "Minecraft Bridge", "minecraft"),
        ("minecraft-player-sync", "Minecraft Player Sync", "minecraft"),
        ("minecraft-rewards", "Minecraft Rewards", "minecraft"),
        ("commerce-provider", "Commerce Provider", "commerce")
    ];

    public static void MapIntegrationEndpoints(this WebApplication app)
    {
        var admin = app.MapGroup("/api/v1/admin/integrations").RequireAuthorization("AdminOnly");
        admin.AddEndpointFilter(async (context, next) =>
        {
            if (HttpMethods.IsGet(context.HttpContext.Request.Method)) return await next(context);
            var antiforgery = context.HttpContext.RequestServices.GetRequiredService<Microsoft.AspNetCore.Antiforgery.IAntiforgery>();
            return await AuthEndpoints.ValidateCsrf(context.HttpContext, antiforgery) ? await next(context) : Results.Problem(statusCode: 400, title: "Invalid anti-forgery token.");
        });
        admin.MapGet("/capabilities", ListAdminAsync);
        admin.MapPatch("/capabilities/{key}", UpdateAsync);
        app.MapGet("/api/v1/integrations/status", ListPublicAsync);
    }

    private static async Task<IResult> ListAdminAsync(NovaDbContext db, CancellationToken ct)
    {
        await EnsureKnownAsync(db, ct);
        var rows = await db.IntegrationCapabilities.AsNoTracking().OrderBy(x => x.CapabilityKey).ToListAsync(ct);
        return Results.Ok(rows.Select(x => new { x.Id, x.CapabilityKey, x.DisplayName, x.Owner, status = StatusName(x.Status), x.LastCheckedAt, x.LastSuccessAt, x.SafeMessage, x.UpdatedAt, etag = ETag(x) }));
    }

    private static async Task<IResult> ListPublicAsync(NovaDbContext db, CancellationToken ct)
    {
        var persisted = await db.IntegrationCapabilities.AsNoTracking().ToListAsync(ct);
        var rows = Known.Select(definition =>
        {
            var capability = persisted.SingleOrDefault(x => x.CapabilityKey == definition.Key);
            return new
            {
                capabilityKey = definition.Key,
                displayName = capability?.DisplayName ?? definition.Name,
                owner = capability?.Owner ?? definition.Owner,
                status = capability is null ? "unavailable" : StatusName(capability.Status),
                lastCheckedAt = capability?.LastCheckedAt,
                lastSuccessAt = capability?.LastSuccessAt,
                safeMessage = capability?.SafeMessage ?? UnavailableMessage
            };
        });
        return Results.Ok(rows);
    }

    private static async Task<IResult> UpdateAsync(NovaDbContext db, HttpContext http, string key, IntegrationUpdateRequest request, CancellationToken ct)
    {
        await EnsureKnownAsync(db, ct);
        var capability = await db.IntegrationCapabilities.SingleOrDefaultAsync(x => x.CapabilityKey == key, ct);
        if (capability is null) return Results.NotFound();
        if (!http.Request.Headers.TryGetValue("If-Match", out var match) || string.IsNullOrWhiteSpace(match.ToString())) return Results.Problem(statusCode: 428, title: "If-Match is required.");
        if (!string.Equals(match.ToString(), ETag(capability), StringComparison.Ordinal)) return Results.Problem(statusCode: 412, title: "Capability changed; reload before editing.");
        if (!Enum.TryParse<IntegrationStatus>(request.Status, true, out var next) || !Enum.IsDefined(next)) return Results.ValidationProblem(new Dictionary<string, string[]> { ["status"] = ["Unsupported integration status."] });
        var safeErrors = IntegrationCapabilityPolicy.ValidateSafeMessage(request.SafeMessage ?? capability.SafeMessage); if (safeErrors.Count > 0) return Results.ValidationProblem(safeErrors);
        if (!IntegrationCapabilityPolicy.CanTransition(capability.Status, next)) return Results.Problem(statusCode: 400, title: "Capability cannot transition to this status without a configured adapter.");
        capability.Status = next; capability.SafeMessage = request.SafeMessage?.Trim() ?? capability.SafeMessage; capability.LastCheckedAt = DateTimeOffset.UtcNow; if (next == IntegrationStatus.Healthy) capability.LastSuccessAt = capability.LastCheckedAt; capability.UpdatedAt = DateTimeOffset.UtcNow;
        AuditWriter.TryAdd(db, http, "integration.capability.updated", "IntegrationCapability", capability.Id, new { capability.CapabilityKey, status = next.ToString().ToLowerInvariant() });
        try { await db.SaveChangesAsync(ct); } catch (DbUpdateConcurrencyException) { return Results.Problem(statusCode: 412, title: "Capability changed; reload before editing."); }
        http.Response.Headers.ETag = ETag(capability); return Results.Ok(new { capability.CapabilityKey, status = StatusName(capability.Status), capability.SafeMessage, etag = ETag(capability) });
    }

    private static async Task EnsureKnownAsync(NovaDbContext db, CancellationToken ct)
    {
        var existing = await db.IntegrationCapabilities.Select(x => x.CapabilityKey).ToListAsync(ct);
        foreach (var definition in Known.Where(x => !existing.Contains(x.Key))) db.IntegrationCapabilities.Add(new IntegrationCapability { CapabilityKey = definition.Key, DisplayName = definition.Name, Owner = definition.Owner, Status = IntegrationStatus.Unavailable, SafeMessage = UnavailableMessage });
        if (Known.Any(x => !existing.Contains(x.Key))) await db.SaveChangesAsync(ct);
    }

    private static string StatusName(IntegrationStatus status) => status.ToString().ToLowerInvariant();
    private static string ETag(IntegrationCapability capability) => $"\"{Convert.ToBase64String(capability.RowVersion)}\"";
}

public sealed record IntegrationUpdateRequest(string? Status, string? SafeMessage);
