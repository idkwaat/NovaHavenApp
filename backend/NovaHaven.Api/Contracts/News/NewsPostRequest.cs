namespace NovaHaven.Api.Contracts.News;

public sealed record NewsPostRequest(string Title, string Slug, string Summary, string Markdown);
