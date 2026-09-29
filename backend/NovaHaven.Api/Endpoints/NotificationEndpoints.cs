using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Features.Notifications;
using NovaHaven.Domain.Notifications.Entities;
using NovaHaven.Infrastructure.Data;
using NovaHaven.Infrastructure.Identity;
using NovaHaven.Infrastructure.Notifications;

namespace NovaHaven.Api.Endpoints;

public static class NotificationEndpoints
{
    private static readonly JsonSerializerOptions PushJson = new(JsonSerializerDefaults.Web);

    public sealed record PushKeys(string? P256dh, string? Auth);
    public sealed record PushInput(string? Endpoint, PushKeys? Keys);
    public sealed record DeletePushInput(string? Endpoint);
    public sealed record AnnouncementInput(string? Title, string? Body, string? Href);

    public static void MapNotificationEndpoints(this WebApplication app)
    {
        var users = app.MapGroup("/api/v1/notifications").WithTags("Notifications").RequireAuthorization();
        users.MapGet("", GetInboxAsync);
        users.MapPut("/{id:guid}/read", MarkReadAsync);
        users.MapPost("/read-all", MarkAllReadAsync);
        users.MapGet("/push/config", async (IWebPushGateway push, CancellationToken ct) =>
            Results.Ok(new { publicKey = await push.GetPublicKeyAsync(ct) }));
        users.MapPut("/push/subscriptions", SavePushSubscriptionAsync);
        users.MapDelete("/push/subscriptions", DeletePushSubscriptionAsync);

        app.MapPost("/api/v1/admin/notifications", SendAnnouncementAsync)
            .WithTags("Admin")
            .RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> GetInboxAsync(HttpContext http, NovaDbContext db, CancellationToken ct)
    {
        if (!TryGetUserId(http.User, out var userId)) return Results.Unauthorized();
        http.Response.Headers.CacheControl = "no-store";
        var items = await db.UserNotifications.AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.CreatedAtUtc)
            .Take(50)
            .Select(x => new { id = x.Id.ToString(), x.Title, x.Body, x.Href, x.CreatedAtUtc, x.ReadAtUtc })
            .ToListAsync(ct);
        var unreadCount = await db.UserNotifications.CountAsync(x => x.UserId == userId && x.ReadAtUtc == null, ct);
        return Results.Ok(new { items, unreadCount });
    }

    private static async Task<IResult> MarkReadAsync(Guid id, HttpContext http, IAntiforgery csrf,
        NovaDbContext db, CancellationToken ct)
    {
        if (!await AuthEndpoints.ValidateCsrf(http, csrf)) return InvalidCsrf();
        if (!TryGetUserId(http.User, out var userId)) return Results.Unauthorized();
        var notification = await db.UserNotifications.SingleOrDefaultAsync(x => x.Id == id && x.UserId == userId, ct);
        if (notification is null) return Results.NotFound();
        notification.MarkRead(DateTimeOffset.UtcNow);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> MarkAllReadAsync(HttpContext http, IAntiforgery csrf,
        NovaDbContext db, CancellationToken ct)
    {
        if (!await AuthEndpoints.ValidateCsrf(http, csrf)) return InvalidCsrf();
        if (!TryGetUserId(http.User, out var userId)) return Results.Unauthorized();
        var unread = await db.UserNotifications.Where(x => x.UserId == userId && x.ReadAtUtc == null).ToListAsync(ct);
        var now = DateTimeOffset.UtcNow;
        foreach (var notification in unread) notification.MarkRead(now);
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SavePushSubscriptionAsync(HttpContext http, IAntiforgery csrf,
        NovaDbContext db, IWebPushGateway push, PushInput input, CancellationToken ct)
    {
        if (!await AuthEndpoints.ValidateCsrf(http, csrf)) return InvalidCsrf();
        if (!TryGetUserId(http.User, out var userId)) return Results.Unauthorized();
        if (!PushEndpointRules.IsSupported(input.Endpoint) || input.Keys is null ||
            !IsBase64Url(input.Keys.P256dh, 128) || !IsBase64Url(input.Keys.Auth, 64))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["subscription"] = ["Thông tin nhận thông báo không hợp lệ."] });
        if (await push.GetPublicKeyAsync(ct) is null)
            return Results.Problem(statusCode: StatusCodes.Status503ServiceUnavailable, title: "Web Push chưa được cấu hình.");

        var endpointHash = PushSubscription.FingerprintEndpoint(input.Endpoint!);
        var existing = await db.PushSubscriptions.SingleOrDefaultAsync(x => x.EndpointHash == endpointHash, ct);
        var now = DateTimeOffset.UtcNow;
        if (existing is not null)
        {
            if (existing.UserId != userId) return Results.Conflict();
            existing.Update(input.Endpoint!, input.Keys.P256dh!, input.Keys.Auth!, now);
        }
        else
        {
            db.PushSubscriptions.Add(PushSubscription.Create(userId, input.Endpoint!, input.Keys.P256dh!, input.Keys.Auth!, now));
        }
        await db.SaveChangesAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> DeletePushSubscriptionAsync(HttpContext http, IAntiforgery csrf,
        NovaDbContext db, [FromBody] DeletePushInput input, CancellationToken ct)
    {
        if (!await AuthEndpoints.ValidateCsrf(http, csrf)) return InvalidCsrf();
        if (!TryGetUserId(http.User, out var userId)) return Results.Unauthorized();
        if (!PushEndpointRules.IsSupported(input.Endpoint)) return Results.NoContent();
        await db.PushSubscriptions.Where(x => x.UserId == userId && x.Endpoint == input.Endpoint).ExecuteDeleteAsync(ct);
        return Results.NoContent();
    }

    private static async Task<IResult> SendAnnouncementAsync(HttpContext http, IAntiforgery csrf,
        NovaDbContext db, UserManager<ApplicationUser> users, IWebPushGateway push,
        AnnouncementInput input, CancellationToken ct)
    {
        if (!await AuthEndpoints.ValidateCsrf(http, csrf)) return InvalidCsrf();
        if (string.IsNullOrWhiteSpace(input.Title) || input.Title.Trim().Length > 120 ||
            string.IsNullOrWhiteSpace(input.Body) || input.Body.Trim().Length > 500 ||
            !IsSafeHref(input.Href))
            return Results.ValidationProblem(new Dictionary<string, string[]> { ["announcement"] = ["Tiêu đề, nội dung hoặc đường dẫn không hợp lệ."] });

        var recipients = await users.Users.AsNoTracking().Where(x => x.EmailConfirmed).Select(x => x.Id).ToListAsync(ct);
        if (recipients.Count > 10_000)
            return Results.Problem(statusCode: StatusCodes.Status413PayloadTooLarge, title: "Danh sách người nhận vượt giới hạn cho bản gửi trực tiếp.");
        var now = DateTimeOffset.UtcNow;
        db.UserNotifications.AddRange(recipients.Select(id => UserNotification.Create(id, input.Title!.Trim(), input.Body!.Trim(), input.Href, now)));
        await db.SaveChangesAsync(ct);

        var subscriptionRows = await db.PushSubscriptions.AsNoTracking().ToListAsync(ct);
        var recipientSet = recipients.ToHashSet();
        var payload = JsonSerializer.Serialize(new { title = input.Title!.Trim(), body = input.Body!.Trim(), href = input.Href }, PushJson);
        var staleIds = new List<Guid>();
        foreach (var subscription in subscriptionRows.Where(x => recipientSet.Contains(x.UserId)))
        {
            var status = await push.SendAsync(subscription, payload, ct);
            if (status == PushDeliveryStatus.Gone) staleIds.Add(subscription.Id);
        }
        if (staleIds.Count > 0)
            await db.PushSubscriptions.Where(x => staleIds.Contains(x.Id)).ExecuteDeleteAsync(ct);

        return Results.Ok(new { recipientCount = recipients.Count, pushAttemptCount = subscriptionRows.Count(x => recipientSet.Contains(x.UserId)) });
    }

    private static bool TryGetUserId(ClaimsPrincipal principal, out Guid userId) =>
        Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out userId);

    private static bool IsBase64Url(string? value, int maxLength) =>
        !string.IsNullOrWhiteSpace(value) && value.Length <= maxLength && value.All(c =>
            c is >= 'A' and <= 'Z' or >= 'a' and <= 'z' or >= '0' and <= '9' or '-' or '_');

    private static bool IsSafeHref(string? href) => href is null ||
        (href.Length <= 200 && href.StartsWith('/') && !href.StartsWith("//", StringComparison.Ordinal) && !href.Contains('\\'));

    private static IResult InvalidCsrf() => Results.Problem(statusCode: StatusCodes.Status400BadRequest, title: "Invalid anti-forgery token.");
}
