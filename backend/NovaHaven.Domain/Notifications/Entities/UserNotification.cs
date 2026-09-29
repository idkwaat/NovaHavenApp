namespace NovaHaven.Domain.Notifications.Entities;

public sealed class UserNotification
{
    private UserNotification() { }

    private UserNotification(Guid userId, string title, string body, string? href, DateTimeOffset createdAtUtc)
    {
        Id = Guid.NewGuid();
        UserId = userId;
        Title = title;
        Body = body;
        Href = href;
        CreatedAtUtc = createdAtUtc.ToUniversalTime();
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Title { get; private set; } = string.Empty;
    public string Body { get; private set; } = string.Empty;
    public string? Href { get; private set; }
    public DateTimeOffset CreatedAtUtc { get; private set; }
    public DateTimeOffset? ReadAtUtc { get; private set; }

    public static UserNotification Create(Guid userId, string title, string body, string? href, DateTimeOffset createdAtUtc)
    {
        if (userId == Guid.Empty) throw new ArgumentException("A recipient is required.", nameof(userId));
        if (string.IsNullOrWhiteSpace(title) || title.Trim().Length > 120) throw new ArgumentException("Title must contain 1–120 characters.", nameof(title));
        if (string.IsNullOrWhiteSpace(body) || body.Trim().Length > 500) throw new ArgumentException("Body must contain 1–500 characters.", nameof(body));
        if (href is not null && (href.Length > 200 || !href.StartsWith('/') || href.StartsWith("//", StringComparison.Ordinal) || href.Contains('\\')))
            throw new ArgumentException("Destination must be a same-site path.", nameof(href));
        return new UserNotification(userId, title.Trim(), body.Trim(), href, createdAtUtc);
    }

    public void MarkRead(DateTimeOffset readAtUtc) => ReadAtUtc ??= readAtUtc.ToUniversalTime();
}
