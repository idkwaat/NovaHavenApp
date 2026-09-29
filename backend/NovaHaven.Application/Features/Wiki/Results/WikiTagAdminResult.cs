namespace NovaHaven.Application.Features.Wiki.Results;

public sealed record WikiTagAdminResult(Guid Id, string Name, string Slug, bool IsActive, byte[] RowVersion);
