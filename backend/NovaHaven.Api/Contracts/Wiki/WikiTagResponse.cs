namespace NovaHaven.Api.Contracts.Wiki;

public sealed record WikiTagResponse(Guid Id, string Name, string Slug, int ArticleCount);
