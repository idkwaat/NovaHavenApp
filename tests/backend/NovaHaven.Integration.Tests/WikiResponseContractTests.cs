using System.Text.Json;
using NovaHaven.Api.Contracts.Wiki;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class WikiResponseContractTests
{
    private static readonly JsonSerializerOptions WebJson = new(JsonSerializerDefaults.Web);

    [Fact]
    public void Article_page_and_summary_keep_the_existing_json_shape()
    {
        var summary = new WikiArticleSummaryResponse(
            Guid.NewGuid(), "star-valley", "Star Valley", "A field guide.", "starting-zone",
            ["guide"], DateTimeOffset.UnixEpoch, 2, null, null);
        var page = new WikiArticlePageResponse([summary], 1, 20, 1);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(page, WebJson));
        var root = document.RootElement;

        AssertProperties(root, "items", "page", "pageSize", "total");
        Assert.Equal(JsonValueKind.String, root.GetProperty("items")[0].GetProperty("id").ValueKind);
        AssertProperties(root.GetProperty("items")[0],
            "id", "slug", "title", "summary", "category", "tags", "publishedAt", "revision",
            "previewImageUrl", "previewImageAlt");
        Assert.Equal(JsonValueKind.Null, root.GetProperty("items")[0].GetProperty("previewImageUrl").ValueKind);
    }

    [Fact]
    public void Article_detail_preserves_media_and_related_response_fields()
    {
        var media = new WikiArticleMediaResponse(
            Guid.NewGuid(), "/api/v1/wiki/media/00000000-0000-0000-0000-000000000001",
            "image/png", "map.png", 32, 32, "A pixel map.");
        var related = new WikiRelatedArticleResponse(
            Guid.NewGuid(), "class-guide", "Class guide", "Choose a path.", "starting-zone", 1,
            DateTimeOffset.UnixEpoch);
        var article = new WikiArticleResponse(
            Guid.NewGuid(), "star-valley", "Star Valley", "A field guide.", "# Star Valley", "starting-zone",
            ["guide"], [media], [related], 2, DateTimeOffset.UnixEpoch);

        using var document = JsonDocument.Parse(JsonSerializer.Serialize(article, WebJson));
        var root = document.RootElement;

        AssertProperties(root,
            "id", "slug", "title", "summary", "markdown", "category", "tags", "media", "related",
            "revision", "publishedAt");
        AssertProperties(root.GetProperty("media")[0],
            "id", "url", "contentType", "originalFileName", "width", "height", "alt");
        AssertProperties(root.GetProperty("related")[0],
            "id", "slug", "title", "summary", "category", "revision", "publishedAt");
    }

    [Fact]
    public void Category_and_tag_json_remain_arrays_of_the_existing_fields()
    {
        var category = JsonSerializer.SerializeToElement(
            new WikiCategoryResponse(Guid.NewGuid(), "Starting Zone", "starting-zone", 0, 4), WebJson);
        var tag = JsonSerializer.SerializeToElement(
            new WikiTagResponse(Guid.NewGuid(), "Guide", "guide", 3), WebJson);

        AssertProperties(category, "id", "name", "slug", "displayOrder", "articleCount");
        AssertProperties(tag, "id", "name", "slug", "articleCount");

        var adminCategory = JsonSerializer.SerializeToElement(
            new AdminWikiCategoryResponse(Guid.NewGuid(), "Starting Zone", "starting-zone", 0, true, "\"etag\""),
            WebJson);
        var adminCategoryDetail = JsonSerializer.SerializeToElement(
            new AdminWikiCategoryDetailResponse(Guid.NewGuid(), "Starting Zone", "starting-zone", 0, true),
            WebJson);
        var adminTag = JsonSerializer.SerializeToElement(
            new AdminWikiTagResponse(Guid.NewGuid(), "Guide", "guide", true, "\"etag\""),
            WebJson);

        AssertProperties(adminCategory, "id", "name", "slug", "displayOrder", "isActive", "etag");
        AssertProperties(adminCategoryDetail, "id", "name", "slug", "displayOrder", "isActive");
        AssertProperties(adminTag, "id", "name", "slug", "isActive", "etag");
    }

    private static void AssertProperties(JsonElement element, params string[] expected)
    {
        Assert.Equal(expected.OrderBy(name => name, StringComparer.Ordinal),
            element.EnumerateObject().Select(property => property.Name).OrderBy(name => name, StringComparer.Ordinal));
    }
}
