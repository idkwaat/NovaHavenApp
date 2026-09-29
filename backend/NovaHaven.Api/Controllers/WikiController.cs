using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NovaHaven.Api.Contracts.Wiki;
using NovaHaven.Api.Errors;
using NovaHaven.Application.Features.Wiki.Queries;
using NovaHaven.Application.Features.Wiki.Results;
using NovaHaven.Application.Features.Wiki.Services;

namespace NovaHaven.Api.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/v1/wiki")]
public sealed class WikiController(WikiReadService wikiReadService) : ControllerBase
{
    [HttpGet("articles")]
    [ProducesResponseType(typeof(WikiArticlePageResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListArticles(
        [FromQuery(Name = "q")] string? query,
        [FromQuery] string? category,
        [FromQuery] string? tag,
        [FromQuery] int? page,
        [FromQuery] int? pageSize,
        CancellationToken cancellationToken)
    {
        var result = await wikiReadService.ListArticlesAsync(
            new WikiArticleListQuery(query, category, tag, page ?? 1, pageSize ?? 20),
            cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);

        var pageResult = result.Value!;
        return Ok(new WikiArticlePageResponse(
            pageResult.Items.Select(ToSummaryResponse).ToArray(),
            pageResult.Page,
            pageResult.PageSize,
            pageResult.Total));
    }

    [HttpGet("articles/{slug}")]
    [ProducesResponseType(typeof(WikiArticleResponse), StatusCodes.Status200OK)]
    public async Task<IActionResult> GetArticle(string slug, CancellationToken cancellationToken)
    {
        var result = await wikiReadService.GetArticleAsync(slug, cancellationToken);
        if (!result.IsSuccess)
        {
            if (result.Error!.Code == "wiki.article.not-found") return NotFound();
            return ApplicationProblemDetailsMapper.ToActionResult(result.Error);
        }

        var article = result.Value!;
        return Ok(new WikiArticleResponse(
            article.Id,
            article.Slug,
            article.Title,
            article.Summary,
            article.Markdown,
            article.Category,
            article.Tags,
            article.Media.Select(media => new WikiArticleMediaResponse(
                media.Id,
                $"/api/v1/wiki/media/{media.Id}",
                media.ContentType,
                media.OriginalFileName,
                media.Width,
                media.Height,
                media.Alt)).ToArray(),
            article.Related.Select(related => new WikiRelatedArticleResponse(
                related.Id,
                related.Slug,
                related.Title,
                related.Summary,
                related.Category,
                related.Revision,
                related.PublishedAt)).ToArray(),
            article.Revision,
            article.PublishedAt));
    }

    [HttpGet("tags")]
    [ProducesResponseType(typeof(IReadOnlyList<WikiTagResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListTags(CancellationToken cancellationToken)
    {
        var result = await wikiReadService.ListTagsAsync(cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);

        return Ok(result.Value!.Select(tag => new WikiTagResponse(
            tag.Id, tag.Name, tag.Slug, tag.ArticleCount)).ToArray());
    }

    [HttpGet("categories")]
    [ProducesResponseType(typeof(IReadOnlyList<WikiCategoryResponse>), StatusCodes.Status200OK)]
    public async Task<IActionResult> ListCategories(CancellationToken cancellationToken)
    {
        var result = await wikiReadService.ListCategoriesAsync(cancellationToken);
        if (!result.IsSuccess) return ApplicationProblemDetailsMapper.ToActionResult(result.Error!);

        return Ok(result.Value!.Select(category => new WikiCategoryResponse(
            category.Id, category.Name, category.Slug, category.DisplayOrder, category.ArticleCount)).ToArray());
    }

    private static WikiArticleSummaryResponse ToSummaryResponse(WikiArticleSummaryResult article) => new(
        article.Id,
        article.Slug,
        article.Title,
        article.Summary,
        article.Category,
        article.Tags,
        article.PublishedAt,
        article.Revision,
        article.PreviewMediaId is Guid mediaId ? $"/api/v1/wiki/media/{mediaId}" : null,
        article.PreviewImageAlt);
}
