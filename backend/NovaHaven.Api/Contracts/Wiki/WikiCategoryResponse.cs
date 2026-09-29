namespace NovaHaven.Api.Contracts.Wiki;

public sealed record WikiCategoryResponse(Guid Id, string Name, string Slug, int DisplayOrder, int ArticleCount);
