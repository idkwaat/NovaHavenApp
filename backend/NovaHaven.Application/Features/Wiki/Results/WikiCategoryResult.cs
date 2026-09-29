namespace NovaHaven.Application.Features.Wiki.Results;

public sealed record WikiCategoryResult(Guid Id, string Name, string Slug, int DisplayOrder, int ArticleCount);
