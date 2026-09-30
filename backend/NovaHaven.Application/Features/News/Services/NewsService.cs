using NovaHaven.Application.Common.Concurrency;
using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Features.News.Commands;
using NovaHaven.Application.Features.News.Queries;
using NovaHaven.Application.Features.News.Repositories;
using NovaHaven.Application.Features.News.Results;
using NovaHaven.Application.Features.News.Validators;
using NovaHaven.Application.Features.Notifications;
using NovaHaven.Domain.News;
using NovaHaven.Domain.News.Entities;

namespace NovaHaven.Application.Features.News.Services;

public sealed class NewsService(
    INewsRepository repository,
    IUnitOfWork unitOfWork,
    IUserNotificationPublisher notificationPublisher)
{
    public async Task<ApplicationResult<IReadOnlyList<AdminNewsListItemResult>>> ListAdminAsync(
        CancellationToken cancellationToken)
    {
        var posts = await repository.ListAdminAsync(cancellationToken);
        return Success<IReadOnlyList<AdminNewsListItemResult>>(posts.Select(ToAdminListItem).ToArray());
    }

    public async Task<ApplicationResult<NewsPageResult>> ListPublishedAsync(
        NewsListQuery query,
        CancellationToken cancellationToken)
    {
        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? 20;
        if (page < 1 || pageSize is < 1 or > 50 || (query.Search?.Length ?? 0) > 100)
            return RequestFailure<NewsPageResult>("Invalid news pagination or filter.");

        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue)
            return RequestFailure<NewsPageResult>("Page is out of range.");

        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var total = await repository.CountPublishedAsync(search, cancellationToken);
        var posts = await repository.ListPublishedAsync(search, (int)offset, pageSize, cancellationToken);
        return Success(new NewsPageResult(posts.Select(ToPublicResult).ToArray(), page, pageSize, total));
    }

    public async Task<ApplicationResult<NewsPostResult>> GetPublishedAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var post = await repository.FindPublishedBySlugAsync(slug, cancellationToken);
        return post is null
            ? NotFound<NewsPostResult>()
            : Success(ToPublicResult(post));
    }

    public async Task<ApplicationResult<NewsWriteResult>> CreateAsync(
        NewsPostInput input,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var errors = NewsValidator.Validate(input);
        if (errors.Count > 0) return ValidationFailure<NewsWriteResult>(errors);
        if (await repository.IsSlugInUseAsync(input.Slug, null, cancellationToken))
            return Conflict<NewsWriteResult>("News slug already exists.");

        var now = DateTimeOffset.UtcNow;
        var post = new NewsPost
        {
            Slug = input.Slug,
            DraftTitle = input.Title.Trim(),
            DraftSummary = input.Summary,
            DraftMarkdown = input.Markdown,
            CreatedAt = now,
            UpdatedAt = now
        };
        repository.Add(post);
        repository.AddAudit(actorId, "news.created", post.Id, new { post.Slug });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(new NewsWriteResult(post.Id, post.Slug, post.RowVersion));
    }

    public async Task<ApplicationResult<AdminNewsPostResult>> GetAdminAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var post = await repository.FindAdminAsync(id, cancellationToken);
        return post is null
            ? NotFound<AdminNewsPostResult>()
            : Success(ToAdminPost(post));
    }

    public async Task<ApplicationResult<NewsWriteResult>> UpdateAsync(
        Guid id,
        NewsPostInput input,
        byte[]? expectedVersion,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var post = await repository.FindForUpdateAsync(id, cancellationToken);
        if (post is null) return NotFound<NewsWriteResult>();
        var precondition = CheckPrecondition(post.RowVersion, expectedVersion);
        if (precondition is not null) return ApplicationResult<NewsWriteResult>.Failure(precondition);

        var errors = NewsValidator.Validate(input);
        if (errors.Count > 0) return ValidationFailure<NewsWriteResult>(errors);
        if (input.Slug != post.Slug && await repository.IsSlugInUseAsync(input.Slug, id, cancellationToken))
            return Conflict<NewsWriteResult>("News slug already exists.");

        post.Slug = input.Slug;
        post.DraftTitle = input.Title.Trim();
        post.DraftSummary = input.Summary;
        post.DraftMarkdown = input.Markdown;
        post.UpdatedAt = DateTimeOffset.UtcNow;
        repository.AddAudit(actorId, "news.edited", post.Id, new { post.Slug });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(new NewsWriteResult(post.Id, post.Slug, post.RowVersion));
    }

    public async Task<ApplicationResult<NewsPublishResult>> PublishAsync(
        Guid id,
        byte[]? expectedVersion,
        Guid publisherId,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var post = await repository.FindForUpdateAsync(id, cancellationToken);
        if (post is null) return NotFound<NewsPublishResult>();
        var precondition = CheckPrecondition(post.RowVersion, expectedVersion);
        if (precondition is not null) return ApplicationResult<NewsPublishResult>.Failure(precondition);

        var publishedAt = DateTimeOffset.UtcNow;
        post.State = NewsState.Published;
        post.PublishedAt = publishedAt;
        post.PublishedBy = publisherId;
        post.UpdatedAt = publishedAt;
        var notification = await notificationPublisher.StageForAllUsersAsync(
            $"Tin mới: {post.DraftTitle}", post.DraftSummary, $"/news/{post.Slug}", cancellationToken);
        repository.AddAudit(actorId, "news.published", post.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await notificationPublisher.DeliverPushAsync(notification, cancellationToken);
        return Success(new NewsPublishResult(post.Id, publishedAt, post.RowVersion));
    }

    public async Task<ApplicationResult<bool>> UnpublishAsync(
        Guid id,
        byte[]? expectedVersion,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var post = await repository.FindForUpdateAsync(id, cancellationToken);
        if (post is null) return NotFound<bool>();
        var precondition = CheckPrecondition(post.RowVersion, expectedVersion);
        if (precondition is not null) return ApplicationResult<bool>.Failure(precondition);
        if (post.State != NewsState.Published)
            return Conflict<bool>("News post is not currently published.");

        post.State = NewsState.Unpublished;
        post.UpdatedAt = DateTimeOffset.UtcNow;
        repository.AddAudit(actorId, "news.unpublished", post.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(true);
    }

    private static ApplicationError? CheckPrecondition(uint rowVersion, byte[]? expectedVersion)
    {
        if (expectedVersion is null)
            return new ApplicationError("http.precondition-required", "If-Match is required.");
        if (!ConcurrencyVersion.Matches(rowVersion, expectedVersion))
            return new ApplicationError("http.precondition-failed", "News post changed; reload before editing.");
        return null;
    }

    private static AdminNewsListItemResult ToAdminListItem(NewsPost post) => new(
        post.Id, post.Slug, post.DraftTitle, post.State, post.PublishedAt, post.UpdatedAt, post.RowVersion);

    private static NewsPostResult ToPublicResult(NewsPost post) => new(
        post.Id, post.Slug, post.DraftTitle, post.DraftSummary, post.DraftMarkdown, post.PublishedAt, post.UpdatedAt);

    private static AdminNewsPostResult ToAdminPost(NewsPost post) => new(
        post.Id, post.Slug, post.DraftTitle, post.DraftSummary, post.DraftMarkdown, post.State, post.PublishedAt, post.RowVersion);

    private static ApplicationResult<T> Success<T>(T value) => ApplicationResult<T>.Success(value);

    private static ApplicationResult<T> NotFound<T>() =>
        Failure<T>("news.post.not-found", "The requested News post was not found.");

    private static ApplicationResult<T> Conflict<T>(string message) => Failure<T>("news.post.conflict", message);

    private static ApplicationResult<T> RequestFailure<T>(string message) =>
        Failure<T>("validation.failed", message);

    private static ApplicationResult<T> ValidationFailure<T>(Dictionary<string, string[]> errors) =>
        Failure<T>("validation.failed", "One or more validation errors occurred.", errors);

    private static ApplicationResult<T> Failure<T>(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? validationErrors = null) =>
        ApplicationResult<T>.Failure(new ApplicationError(code, message, validationErrors));
}
