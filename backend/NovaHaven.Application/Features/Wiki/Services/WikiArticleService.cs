using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Common.Concurrency;
using NovaHaven.Application.Features.Notifications;
using NovaHaven.Application.Features.Wiki.Repositories;
using NovaHaven.Application.Features.Wiki.Results;
using NovaHaven.Application.Wiki;
using NovaHaven.Domain.Wiki.Entities;

namespace NovaHaven.Application.Features.Wiki.Services;

public sealed class WikiArticleService(
    IWikiArticleRepository repository,
    IUnitOfWork unitOfWork,
    IUserNotificationPublisher notificationPublisher)
{
    public async Task<ApplicationResult<IReadOnlyList<WikiArticleAdminListItemResult>>> ListAsync(
        CancellationToken cancellationToken)
    {
        var articles = await repository.ListAsync(cancellationToken);
        return ApplicationResult<IReadOnlyList<WikiArticleAdminListItemResult>>.Success(articles);
    }

    public async Task<ApplicationResult<WikiArticleDraftResult>> GetDraftAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var article = await repository.FindDraftAsync(id, cancellationToken);
        return article is null
            ? NotFound<WikiArticleDraftResult>()
            : ApplicationResult<WikiArticleDraftResult>.Success(article);
    }

    public async Task<ApplicationResult<WikiArticleWriteResult>> CreateAsync(
        WikiDraftInput input,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var errors = WikiDraftValidator.Validate(input);
        if (errors.Count > 0) return ValidationFailure<WikiArticleWriteResult>(errors);

        return await unitOfWork.ExecuteInTransactionAsync(
            async transactionToken =>
            {
                var tagIds = input.TagIds ?? [];
                var mediaIds = input.MediaIds ?? [];
                if (!await repository.IsActiveCategoryAsync(input.CategoryId, transactionToken))
                    return InvalidCategory<WikiArticleWriteResult>();
                if (!await repository.AreActiveTagsAsync(tagIds, transactionToken))
                    return InvalidTags<WikiArticleWriteResult>();
                if (!await repository.AreExistingMediaAsync(mediaIds, transactionToken))
                    return InvalidMedia<WikiArticleWriteResult>();
                if (await repository.IsSlugReservedAsync(input.Slug, null, transactionToken))
                    return Conflict<WikiArticleWriteResult>("Article slug is reserved.");

                var now = DateTimeOffset.UtcNow;
                var article = new WikiArticle
                {
                    Slug = input.Slug,
                    DraftTitle = input.Title.Trim(),
                    DraftSummary = input.Summary,
                    DraftMarkdown = input.Markdown,
                    DraftCategoryId = input.CategoryId,
                    CreatedAt = now,
                    UpdatedAt = now
                };
                repository.Add(article);
                repository.AddDraftTags(article.Id, tagIds);
                repository.AddDraftMedia(article.Id, mediaIds);
                repository.AddAudit(actorId, "article.created", article.Id,
                    new { article.Slug, mediaCount = mediaIds.Length });
                await unitOfWork.SaveChangesAsync(transactionToken);
                return ApplicationResult<WikiArticleWriteResult>.Success(
                    new WikiArticleWriteResult(article.Id, article.Slug, ConcurrencyVersion.ToBytes(article.RowVersion)));
            },
            result => result.IsSuccess,
            TransactionIsolation.Serializable,
            cancellationToken);
    }

    public Task<ApplicationResult<WikiArticleWriteResult>> UpdateAsync(
        Guid id,
        WikiDraftInput input,
        byte[]? expectedVersion,
        Guid? actorId,
        CancellationToken cancellationToken) => unitOfWork.ExecuteInTransactionAsync(
        async transactionToken =>
        {
            var article = await repository.FindForUpdateAsync(id, transactionToken);
            if (article is null) return NotFound<WikiArticleWriteResult>();
            var precondition = CheckPrecondition(article.RowVersion, expectedVersion);
            if (precondition is not null) return ApplicationResult<WikiArticleWriteResult>.Failure(precondition);

            var errors = WikiDraftValidator.Validate(input);
            if (errors.Count > 0) return ValidationFailure<WikiArticleWriteResult>(errors);
            if (article.WasPublished && input.Slug != article.Slug)
                return Conflict<WikiArticleWriteResult>("A previously published slug cannot change.");

            var tagIds = input.TagIds ?? [];
            var mediaIds = input.MediaIds ?? [];
            if (!await repository.IsActiveCategoryAsync(input.CategoryId, transactionToken))
                return InvalidCategory<WikiArticleWriteResult>();
            if (!await repository.AreActiveTagsAsync(tagIds, transactionToken))
                return InvalidTags<WikiArticleWriteResult>();
            if (!await repository.AreExistingMediaAsync(mediaIds, transactionToken))
                return InvalidMedia<WikiArticleWriteResult>();
            if (await repository.IsSlugReservedAsync(input.Slug, id, transactionToken))
                return Conflict<WikiArticleWriteResult>("Article slug is reserved.");

            article.Slug = input.Slug;
            article.DraftTitle = input.Title.Trim();
            article.DraftSummary = input.Summary;
            article.DraftMarkdown = input.Markdown;
            article.DraftCategoryId = input.CategoryId;
            article.UpdatedAt = DateTimeOffset.UtcNow;
            await repository.ReplaceDraftTagsAsync(article.Id, tagIds, transactionToken);
            await repository.ReplaceDraftMediaAsync(article.Id, mediaIds, transactionToken);
            repository.AddAudit(actorId, "article.edited", article.Id,
                new { article.Slug, mediaCount = mediaIds.Length });
            await unitOfWork.SaveChangesAsync(transactionToken);
            return ApplicationResult<WikiArticleWriteResult>.Success(
                new WikiArticleWriteResult(article.Id, article.Slug, ConcurrencyVersion.ToBytes(article.RowVersion)));
        },
        result => result.IsSuccess,
        TransactionIsolation.Serializable,
        cancellationToken);

    public async Task<ApplicationResult<WikiArticlePublishedResult>> PublishAsync(
        Guid id,
        byte[]? expectedVersion,
        Guid publisherId,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        NotificationBatch? notification = null;
        var result = await unitOfWork.ExecuteInTransactionAsync(
        async transactionToken =>
        {
            var article = await repository.FindForUpdateAsync(id, transactionToken);
            if (article is null) return NotFound<WikiArticlePublishedResult>();
            var precondition = CheckPrecondition(article.RowVersion, expectedVersion);
            if (precondition is not null) return ApplicationResult<WikiArticlePublishedResult>.Failure(precondition);

            var errors = WikiDraftValidator.Validate(new WikiDraftInput(
                article.DraftTitle, article.Slug, article.DraftSummary, article.DraftMarkdown, article.DraftCategoryId));
            if (errors.Count > 0) return ValidationFailure<WikiArticlePublishedResult>(errors);
            if (!await repository.IsActiveCategoryAsync(article.DraftCategoryId, transactionToken))
                return InvalidCategory<WikiArticlePublishedResult>();

            var tagIds = await repository.GetDraftTagIdsAsync(id, transactionToken);
            if (!await repository.AreActiveTagsAsync(tagIds, transactionToken))
                return InvalidTags<WikiArticlePublishedResult>();
            var mediaIds = await repository.GetDraftMediaIdsAsync(id, transactionToken);
            if (!await repository.AreExistingMediaAsync(mediaIds, transactionToken))
                return InvalidMedia<WikiArticlePublishedResult>();

            var revision = WikiArticleRevision.FromDraft(article, publisherId, DateTimeOffset.UtcNow);
            repository.AddPublishedSnapshot(revision, tagIds, mediaIds);
            article.PublishedRevisionId = revision.Id;
            article.LatestRevisionNumber = revision.Number;
            article.WasPublished = true;
            article.State = ArticleState.Published;
            article.UpdatedAt = revision.PublishedAt;
            repository.AddAudit(actorId, "article.published", article.Id,
                new { revisionId = revision.Id, revision = revision.Number });
            notification = await notificationPublisher.StageForAllUsersAsync(
                $"Wiki mới: {article.DraftTitle}", article.DraftSummary, $"/wiki/{article.Slug}", transactionToken);
            await unitOfWork.SaveChangesAsync(transactionToken);
            return ApplicationResult<WikiArticlePublishedResult>.Success(new WikiArticlePublishedResult(
                article.Id, article.Slug, revision.Id, revision.Number, ConcurrencyVersion.ToBytes(article.RowVersion)));
        },
        result => result.IsSuccess,
        TransactionIsolation.Serializable,
        cancellationToken);
        if (result.IsSuccess && notification is not null)
            await notificationPublisher.DeliverPushAsync(notification, cancellationToken);
        return result;
    }

    public Task<ApplicationResult<bool>> UnpublishAsync(
        Guid id,
        byte[]? expectedVersion,
        Guid? actorId,
        CancellationToken cancellationToken) =>
        UnpublishInTransactionAsync(id, expectedVersion, actorId, cancellationToken);

    public async Task<ApplicationResult<IReadOnlyList<WikiArticleRevisionResult>>> ListRevisionsAsync(
        Guid articleId,
        CancellationToken cancellationToken)
    {
        if (!await repository.ExistsAsync(articleId, cancellationToken))
            return NotFound<IReadOnlyList<WikiArticleRevisionResult>>();

        var revisions = await repository.ListRevisionsAsync(articleId, cancellationToken);
        return ApplicationResult<IReadOnlyList<WikiArticleRevisionResult>>.Success(revisions);
    }

    public Task<ApplicationResult<WikiArticleWriteResult>> RestoreAsync(
        Guid id,
        Guid revisionId,
        byte[]? expectedVersion,
        Guid? actorId,
        CancellationToken cancellationToken) => unitOfWork.ExecuteInTransactionAsync(
        async transactionToken =>
        {
            var article = await repository.FindForUpdateAsync(id, transactionToken);
            if (article is null) return NotFound<WikiArticleWriteResult>();
            var precondition = CheckPrecondition(article.RowVersion, expectedVersion);
            if (precondition is not null) return ApplicationResult<WikiArticleWriteResult>.Failure(precondition);

            var revision = await repository.FindRevisionAsync(id, revisionId, transactionToken);
            if (revision is null) return NotFound<WikiArticleWriteResult>();
            var tagIds = await repository.GetRevisionTagIdsAsync(revisionId, transactionToken);
            if (!await repository.AreActiveTagsAsync(tagIds, transactionToken))
                return InvalidTags<WikiArticleWriteResult>();
            var mediaIds = await repository.GetRevisionMediaIdsAsync(revisionId, transactionToken);
            if (!await repository.AreExistingMediaAsync(mediaIds, transactionToken))
                return InvalidMedia<WikiArticleWriteResult>();

            await repository.ReplaceDraftTagsAsync(id, tagIds, transactionToken);
            await repository.ReplaceDraftMediaAsync(id, mediaIds, transactionToken);
            article.DraftTitle = revision.Title;
            article.DraftSummary = revision.Summary;
            article.DraftMarkdown = revision.Markdown;
            article.DraftCategoryId = revision.CategoryId;
            article.UpdatedAt = DateTimeOffset.UtcNow;
            repository.AddAudit(actorId, "article.revision_restored", article.Id,
                new { revisionId, revision = revision.Number });
            await unitOfWork.SaveChangesAsync(transactionToken);
            return ApplicationResult<WikiArticleWriteResult>.Success(
                new WikiArticleWriteResult(article.Id, article.Slug, ConcurrencyVersion.ToBytes(article.RowVersion)));
        },
        result => result.IsSuccess,
        TransactionIsolation.Serializable,
        cancellationToken);

    private async Task<ApplicationResult<bool>> UnpublishInTransactionAsync(
        Guid id,
        byte[]? expectedVersion,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var article = await repository.FindForUpdateAsync(id, cancellationToken);
        if (article is null) return NotFound<bool>();
        var precondition = CheckPrecondition(article.RowVersion, expectedVersion);
        if (precondition is not null) return ApplicationResult<bool>.Failure(precondition);
        if (article.State != ArticleState.Published)
            return Conflict<bool>("Article is not currently published.");

        article.State = ArticleState.Unpublished;
        article.UpdatedAt = DateTimeOffset.UtcNow;
        repository.AddAudit(actorId, "article.unpublished", article.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<bool>.Success(true);
    }

    private static ApplicationError? CheckPrecondition(uint rowVersion, byte[]? expectedVersion)
    {
        if (expectedVersion is null)
            return new ApplicationError("http.precondition-required", "If-Match is required.");
        if (!ConcurrencyVersion.Matches(rowVersion, expectedVersion))
            return new ApplicationError("http.precondition-failed", "Article changed; reload before editing.");
        return null;
    }

    private static ApplicationResult<T> NotFound<T>() =>
        Failure<T>("wiki.article.not-found", "The requested article was not found.");

    private static ApplicationResult<T> Conflict<T>(string message) =>
        Failure<T>("wiki.article.conflict", message);

    private static ApplicationResult<T> InvalidCategory<T>() =>
        ValidationFailure<T>(new Dictionary<string, string[]> { ["categoryId"] = ["Choose an active category."] });

    private static ApplicationResult<T> InvalidTags<T>() =>
        ValidationFailure<T>(new Dictionary<string, string[]> { ["tagIds"] = ["Choose at most 10 distinct active tags."] });

    private static ApplicationResult<T> InvalidMedia<T>() =>
        ValidationFailure<T>(new Dictionary<string, string[]> { ["mediaIds"] = ["Choose at most 20 existing media files."] });

    private static ApplicationResult<T> ValidationFailure<T>(Dictionary<string, string[]> errors) =>
        Failure<T>("validation.failed", "One or more validation errors occurred.", errors);

    private static ApplicationResult<T> Failure<T>(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? validationErrors = null) =>
        ApplicationResult<T>.Failure(new ApplicationError(code, message, validationErrors));
}
