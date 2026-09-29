namespace NovaHaven.Application.Features.Wiki.Results;

public sealed record WikiTagResult(Guid Id, string Name, string Slug, int ArticleCount);
