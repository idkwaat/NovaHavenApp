using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Features.Wiki.Repositories;
using NovaHaven.Application.Features.Wiki.Results;
using NovaHaven.Application.Features.Wiki.Services;
using NovaHaven.Application.Wiki;
using NovaHaven.Domain.Wiki.Entities;
using Xunit;

namespace NovaHaven.Domain.Tests;

public sealed class WikiArticleServiceTests
{
    [Fact]
    public async Task Create_rejects_invalid_draft_before_opening_transaction()
    {
        var repository = new StubArticleRepository();
        var unitOfWork = new StubUnitOfWork();
        var service = new WikiArticleService(repository, unitOfWork);

        var result = await service.CreateAsync(
            new WikiDraftInput("", "Bad Slug", "", "", Guid.Empty), null, CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("validation.failed", result.Error!.Code);
        Assert.Contains("title", result.Error.ValidationErrors!.Keys);
        Assert.Equal(0, unitOfWork.TransactionCount);
        Assert.Null(repository.AddedArticle);
    }

    [Fact]
    public async Task Update_requires_matching_etag_and_rolls_back_typed_failure()
    {
        var article = Article();
        var repository = new StubArticleRepository(article);
        var unitOfWork = new StubUnitOfWork();
        var service = new WikiArticleService(repository, unitOfWork);
        var input = ValidInput(article.DraftCategoryId);

        var missing = await service.UpdateAsync(article.Id, input, null, null, CancellationToken.None);
        var stale = await service.UpdateAsync(article.Id, input, [9], null, CancellationToken.None);

        Assert.Equal("http.precondition-required", missing.Error!.Code);
        Assert.Equal("http.precondition-failed", stale.Error!.Code);
        Assert.Equal(2, unitOfWork.TransactionCount);
        Assert.False(unitOfWork.LastTransactionCommitted);
        Assert.Equal(0, unitOfWork.SaveCount);
        Assert.Equal("Existing draft", article.DraftTitle);
    }

    [Fact]
    public async Task Publish_commits_revision_pointer_and_immutable_tag_media_snapshot_together()
    {
        var article = Article();
        var tagId = Guid.NewGuid();
        var mediaId = Guid.NewGuid();
        var repository = new StubArticleRepository(article)
        {
            DraftTagIds = [tagId],
            DraftMediaIds = [mediaId]
        };
        var unitOfWork = new StubUnitOfWork();
        var service = new WikiArticleService(repository, unitOfWork);
        var actorId = Guid.NewGuid();

        var result = await service.PublishAsync(
            article.Id, article.RowVersion, actorId, actorId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.True(unitOfWork.LastTransactionCommitted);
        Assert.Equal(TransactionIsolation.Serializable, unitOfWork.LastIsolation);
        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.Equal(ArticleState.Published, article.State);
        Assert.True(article.WasPublished);
        Assert.Equal(1, article.LatestRevisionNumber);
        Assert.Equal(result.Value!.RevisionId, article.PublishedRevisionId);
        Assert.Equal([tagId], repository.SnapshotTagIds);
        Assert.Equal([mediaId], repository.SnapshotMediaIds);
        Assert.Equal("article.published", repository.LastAuditAction);
    }

    [Fact]
    public async Task Restore_copies_revision_to_draft_without_changing_current_published_pointer()
    {
        var article = Article();
        article.State = ArticleState.Published;
        article.WasPublished = true;
        article.LatestRevisionNumber = 2;
        var publishedRevisionId = Guid.NewGuid();
        article.PublishedRevisionId = publishedRevisionId;
        var restoreId = Guid.NewGuid();
        var restoredCategoryId = Guid.NewGuid();
        var restoredTagId = Guid.NewGuid();
        var restoredMediaId = Guid.NewGuid();
        var repository = new StubArticleRepository(article)
        {
            Revision = new WikiArticleRevision
            {
                Id = restoreId,
                ArticleId = article.Id,
                Number = 1,
                Title = "Earlier title",
                Summary = "Earlier summary",
                Markdown = "Earlier content",
                CategoryId = restoredCategoryId
            },
            RevisionTagIds = [restoredTagId],
            RevisionMediaIds = [restoredMediaId]
        };
        var unitOfWork = new StubUnitOfWork();
        var service = new WikiArticleService(repository, unitOfWork);

        var result = await service.RestoreAsync(
            article.Id, restoreId, article.RowVersion, null, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("Earlier title", article.DraftTitle);
        Assert.Equal("Earlier summary", article.DraftSummary);
        Assert.Equal("Earlier content", article.DraftMarkdown);
        Assert.Equal(restoredCategoryId, article.DraftCategoryId);
        Assert.Equal(publishedRevisionId, article.PublishedRevisionId);
        Assert.Equal(ArticleState.Published, article.State);
        Assert.Equal([restoredTagId], repository.ReplacedTagIds);
        Assert.Equal([restoredMediaId], repository.ReplacedMediaIds);
        Assert.Equal("article.revision_restored", repository.LastAuditAction);
    }

    private static WikiArticle Article() => new()
    {
        Slug = "existing-draft",
        DraftTitle = "Existing draft",
        DraftSummary = "Summary",
        DraftMarkdown = "Content",
        DraftCategoryId = Guid.NewGuid(),
        RowVersion = [1, 2, 3]
    };

    private static WikiDraftInput ValidInput(Guid categoryId) =>
        new("Updated draft", "updated-draft", "Updated summary", "Updated content", categoryId,
            [Guid.NewGuid()], [Guid.NewGuid()]);

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public int TransactionCount { get; private set; }
        public int SaveCount { get; private set; }
        public bool LastTransactionCommitted { get; private set; }
        public TransactionIsolation? LastIsolation { get; private set; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.FromResult(1);
        }

        public async Task<T> ExecuteInTransactionAsync<T>(
            Func<CancellationToken, Task<T>> operation,
            Func<T, bool> shouldCommit,
            TransactionIsolation isolation,
            CancellationToken cancellationToken)
        {
            TransactionCount++;
            LastIsolation = isolation;
            var result = await operation(cancellationToken);
            LastTransactionCommitted = shouldCommit(result);
            return result;
        }
    }

    private sealed class StubArticleRepository(WikiArticle? article = null) : IWikiArticleRepository
    {
        public WikiArticle? AddedArticle { get; private set; }
        public WikiArticleRevision? Revision { get; set; }
        public Guid[] DraftTagIds { get; set; } = [];
        public Guid[] DraftMediaIds { get; set; } = [];
        public Guid[] RevisionTagIds { get; set; } = [];
        public Guid[] RevisionMediaIds { get; set; } = [];
        public Guid[] SnapshotTagIds { get; private set; } = [];
        public Guid[] SnapshotMediaIds { get; private set; } = [];
        public Guid[] ReplacedTagIds { get; private set; } = [];
        public Guid[] ReplacedMediaIds { get; private set; } = [];
        public string? LastAuditAction { get; private set; }

        public Task<IReadOnlyList<WikiArticleAdminListItemResult>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<WikiArticleAdminListItemResult>>([]);

        public Task<WikiArticleDraftResult?> FindDraftAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult<WikiArticleDraftResult?>(null);

        public Task<WikiArticle?> FindForUpdateAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(article?.Id == id ? article : null);

        public Task<bool> ExistsAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(article?.Id == id);

        public Task<WikiArticleRevision?> FindRevisionAsync(Guid articleId, Guid revisionId,
            CancellationToken cancellationToken) =>
            Task.FromResult(article?.Id == articleId && Revision?.Id == revisionId ? Revision : null);

        public Task<IReadOnlyList<WikiArticleRevisionResult>> ListRevisionsAsync(Guid articleId,
            CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<WikiArticleRevisionResult>>([]);

        public Task<bool> IsSlugReservedAsync(string slug, Guid? excludingArticleId,
            CancellationToken cancellationToken) => Task.FromResult(false);

        public Task<bool> IsActiveCategoryAsync(Guid categoryId, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<bool> AreActiveTagsAsync(IReadOnlyCollection<Guid> tagIds, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<bool> AreExistingMediaAsync(IReadOnlyCollection<Guid> mediaIds, CancellationToken cancellationToken) =>
            Task.FromResult(true);

        public Task<Guid[]> GetDraftTagIdsAsync(Guid articleId, CancellationToken cancellationToken) =>
            Task.FromResult(DraftTagIds);

        public Task<Guid[]> GetDraftMediaIdsAsync(Guid articleId, CancellationToken cancellationToken) =>
            Task.FromResult(DraftMediaIds);

        public Task<Guid[]> GetRevisionTagIdsAsync(Guid revisionId, CancellationToken cancellationToken) =>
            Task.FromResult(RevisionTagIds);

        public Task<Guid[]> GetRevisionMediaIdsAsync(Guid revisionId, CancellationToken cancellationToken) =>
            Task.FromResult(RevisionMediaIds);

        public void Add(WikiArticle value) => AddedArticle = value;

        public void AddDraftTags(Guid articleId, IReadOnlyCollection<Guid> tagIds) { }

        public void AddDraftMedia(Guid articleId, IReadOnlyCollection<Guid> mediaIds) { }

        public Task ReplaceDraftTagsAsync(Guid articleId, IReadOnlyCollection<Guid> tagIds,
            CancellationToken cancellationToken)
        {
            ReplacedTagIds = tagIds.ToArray();
            return Task.CompletedTask;
        }

        public Task ReplaceDraftMediaAsync(Guid articleId, IReadOnlyCollection<Guid> mediaIds,
            CancellationToken cancellationToken)
        {
            ReplacedMediaIds = mediaIds.ToArray();
            return Task.CompletedTask;
        }

        public void AddPublishedSnapshot(WikiArticleRevision revision, IReadOnlyCollection<Guid> tagIds,
            IReadOnlyCollection<Guid> mediaIds)
        {
            Revision = revision;
            SnapshotTagIds = tagIds.ToArray();
            SnapshotMediaIds = mediaIds.ToArray();
        }

        public void AddAudit(Guid? actorId, string action, Guid articleId, object? details = null) =>
            LastAuditAction = action;
    }
}
