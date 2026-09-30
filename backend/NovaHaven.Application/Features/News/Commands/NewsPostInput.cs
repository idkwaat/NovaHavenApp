namespace NovaHaven.Application.Features.News.Commands;

public sealed record NewsPostInput(string Title, string Slug, string Summary, string Markdown);
