namespace NovaHaven.Application.Features.Wiki.Results;

public sealed record WikiArticleWriteResult(Guid Id, string Slug, byte[] RowVersion);
