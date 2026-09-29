using NovaHaven.Domain.Notifications.Entities;
using Xunit;

namespace NovaHaven.Domain.Tests;

public sealed class UserNotificationTests
{
    [Fact]
    public void Creates_notification_with_normalized_text_and_a_local_destination()
    {
        var now = DateTimeOffset.Parse("2026-09-29T12:00:00+07:00");
        var item = UserNotification.Create(Guid.NewGuid(), "  Tin mới  ", "  Chào người chơi  ", "/news", now);

        Assert.Equal("Tin mới", item.Title);
        Assert.Equal("Chào người chơi", item.Body);
        Assert.Equal("/news", item.Href);
        Assert.Equal(now.ToUniversalTime(), item.CreatedAtUtc);
        Assert.Null(item.ReadAtUtc);
    }

    [Theory]
    [InlineData("https://outside.example")]
    [InlineData("//outside.example")]
    [InlineData("/\\outside.example")]
    public void Rejects_external_or_ambiguous_destinations(string href) =>
        Assert.Throws<ArgumentException>(() => UserNotification.Create(Guid.NewGuid(), "Tin", "Nội dung", href, DateTimeOffset.UtcNow));

    [Fact]
    public void Mark_read_is_idempotent_and_preserves_first_read_time()
    {
        var item = UserNotification.Create(Guid.NewGuid(), "Tin", "Nội dung", null, DateTimeOffset.UtcNow);
        var first = DateTimeOffset.Parse("2026-09-29T12:00:00Z");
        item.MarkRead(first);
        item.MarkRead(first.AddMinutes(5));

        Assert.Equal(first, item.ReadAtUtc);
    }
}
