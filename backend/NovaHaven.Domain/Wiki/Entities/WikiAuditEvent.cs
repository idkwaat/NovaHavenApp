namespace NovaHaven.Domain.Wiki.Entities;

public sealed class WikiAuditEvent
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ActorUserId { get; set; }
    public string Action { get; set; } = "";
    public string EntityType { get; set; } = "";
    public Guid EntityId { get; set; }
    public string DetailsJson { get; set; } = "{}";
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}
