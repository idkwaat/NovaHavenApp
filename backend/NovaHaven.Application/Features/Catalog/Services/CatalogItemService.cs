using NovaHaven.Application.Common.Concurrency;
using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Features.Catalog.Commands;
using NovaHaven.Application.Features.Catalog.Queries;
using NovaHaven.Application.Features.Catalog.Repositories;
using NovaHaven.Application.Features.Catalog.Results;
using NovaHaven.Application.Features.Catalog.Validators;
using NovaHaven.Application.Features.Notifications;
using NovaHaven.Domain.Catalog.Entities;

namespace NovaHaven.Application.Features.Catalog.Services;

public sealed class CatalogItemService(
    ICatalogItemRepository repository,
    IUnitOfWork unitOfWork,
    IUserNotificationPublisher notificationPublisher)
{
    public async Task<ApplicationResult<CatalogItemPageResult>> ListPublishedAsync(
        CatalogItemListQuery query,
        CancellationToken cancellationToken)
    {
        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? 20;
        if (page < 1 || pageSize is < 1 or > 50 || (query.Search?.Length ?? 0) > 100)
            return RequestFailure<CatalogItemPageResult>("Invalid catalog pagination or filter.");
        if (!TryParseKind(query.Kind, out var kind))
            return RequestFailure<CatalogItemPageResult>("Invalid catalog item kind.");

        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue)
            return RequestFailure<CatalogItemPageResult>("Page is out of range.");

        var search = NormalizeSearch(query.Search);
        var total = await repository.CountPublishedAsync(search, kind, cancellationToken);
        var items = await repository.ListPublishedAsync(search, kind, (int)offset, pageSize, cancellationToken);
        return Success(new CatalogItemPageResult(items, page, pageSize, total));
    }

    public async Task<ApplicationResult<CatalogItemDetailResult>> GetPublishedAsync(
        string slug,
        CancellationToken cancellationToken)
    {
        var item = await repository.FindPublishedBySlugAsync(slug, cancellationToken);
        return item is null ? NotFound<CatalogItemDetailResult>() : Success(item);
    }

    public async Task<ApplicationResult<IReadOnlyList<CatalogItemAdminListItemResult>>> ListAdminAsync(
        CancellationToken cancellationToken)
    {
        var items = await repository.ListAdminAsync(cancellationToken);
        return Success(items);
    }

    public async Task<ApplicationResult<CatalogItemAdminDraftResult>> GetAdminDraftAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var item = await repository.FindAdminDraftAsync(id, cancellationToken);
        return item is null ? NotFound<CatalogItemAdminDraftResult>() : Success(item);
    }

    public async Task<ApplicationResult<CatalogItemWriteResult>> CreateAsync(
        CatalogItemDraftInput draft,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        if (!TryCreateInput(draft, out var input, out var parseErrors))
            return ValidationFailure<CatalogItemWriteResult>(parseErrors);

        var errors = CatalogItemValidator.Validate(input);
        if (errors.Count > 0) return ValidationFailure<CatalogItemWriteResult>(errors);
        if (await repository.IsSlugInUseAsync(input.Slug, null, cancellationToken))
            return Conflict<CatalogItemWriteResult>("Catalog slug already exists.");

        var now = DateTimeOffset.UtcNow;
        var item = new GameCatalogItem
        {
            Slug = input.Slug,
            DraftName = input.Name.Trim(),
            DraftSummary = input.Summary,
            DraftMarkdown = input.Markdown,
            DraftKind = input.Kind,
            UpdatedAt = now
        };
        repository.Add(item);
        repository.AddAudit(actorId, "catalog_item.created", item.Id, new { item.Slug });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(new CatalogItemWriteResult(item.Id, item.Slug, item.RowVersion));
    }

    public async Task<ApplicationResult<CatalogItemWriteResult>> UpdateAsync(
        Guid id,
        CatalogItemDraftInput draft,
        byte[]? expectedVersion,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var item = await repository.FindForUpdateAsync(id, cancellationToken);
        if (item is null) return NotFound<CatalogItemWriteResult>();
        var precondition = CheckPrecondition(item.RowVersion, expectedVersion);
        if (precondition is not null) return ApplicationResult<CatalogItemWriteResult>.Failure(precondition);
        if (!TryCreateInput(draft, out var input, out var parseErrors))
            return ValidationFailure<CatalogItemWriteResult>(parseErrors);

        var errors = CatalogItemValidator.Validate(input);
        if (errors.Count > 0) return ValidationFailure<CatalogItemWriteResult>(errors);
        if (input.Slug != item.Slug && await repository.IsSlugInUseAsync(input.Slug, id, cancellationToken))
            return Conflict<CatalogItemWriteResult>("Catalog slug already exists.");

        item.Slug = input.Slug;
        item.DraftName = input.Name.Trim();
        item.DraftSummary = input.Summary;
        item.DraftMarkdown = input.Markdown;
        item.DraftKind = input.Kind;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        repository.AddAudit(actorId, "catalog_item.edited", item.Id, new { item.Slug });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(new CatalogItemWriteResult(item.Id, item.Slug, item.RowVersion));
    }

    public async Task<ApplicationResult<CatalogItemPublishResult>> PublishAsync(
        Guid id,
        byte[]? expectedVersion,
        Guid publisherId,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var item = await repository.FindForUpdateAsync(id, cancellationToken);
        if (item is null) return NotFound<CatalogItemPublishResult>();
        var precondition = CheckPrecondition(item.RowVersion, expectedVersion);
        if (precondition is not null) return ApplicationResult<CatalogItemPublishResult>.Failure(precondition);

        var errors = CatalogItemValidator.Validate(new CatalogItemInput(
            item.DraftName, item.Slug, item.DraftSummary, item.DraftMarkdown, item.DraftKind));
        if (errors.Count > 0) return ValidationFailure<CatalogItemPublishResult>(errors);

        var publishedAt = DateTimeOffset.UtcNow;
        var revision = GameCatalogItemRevision.FromDraft(item, publisherId, publishedAt);
        item.State = CatalogItemState.Published;
        item.WasPublished = true;
        item.PublishedRevisionId = revision.Id;
        item.LatestRevisionNumber = revision.Number;
        item.UpdatedAt = publishedAt;
        repository.AddRevision(revision);
        var notification = await notificationPublisher.StageForAllUsersAsync(
            $"Vật phẩm mới: {item.DraftName}", item.DraftSummary, $"/catalog/{item.Slug}", cancellationToken);
        repository.AddAudit(actorId, "catalog_item.published", item.Id, new { revision = revision.Number });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await notificationPublisher.DeliverPushAsync(notification, cancellationToken);
        return Success(new CatalogItemPublishResult(item.Id, revision.Id, revision.Number, revision.PublishedAt, item.RowVersion));
    }

    public async Task<ApplicationResult<uint>> UnpublishAsync(
        Guid id,
        byte[]? expectedVersion,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        var item = await repository.FindForUpdateAsync(id, cancellationToken);
        if (item is null) return NotFound<uint>();
        var precondition = CheckPrecondition(item.RowVersion, expectedVersion);
        if (precondition is not null) return ApplicationResult<uint>.Failure(precondition);
        if (item.State != CatalogItemState.Published)
            return Conflict<uint>("Catalog item is not currently published.");

        item.State = CatalogItemState.Unpublished;
        item.UpdatedAt = DateTimeOffset.UtcNow;
        repository.AddAudit(actorId, "catalog_item.unpublished", item.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(item.RowVersion);
    }

    private static bool TryCreateInput(
        CatalogItemDraftInput draft,
        out CatalogItemInput input,
        out Dictionary<string, string[]> errors)
    {
        errors = [];
        if (!Enum.TryParse<CatalogItemKind>(draft.Kind, true, out var kind) || !Enum.IsDefined(kind))
        {
            input = default!;
            errors["kind"] = ["Kind is not supported."];
            return false;
        }

        input = new CatalogItemInput(draft.Name ?? "", draft.Slug ?? "", draft.Summary ?? "", draft.Markdown ?? "", kind);
        return true;
    }

    private static bool TryParseKind(string? value, out CatalogItemKind? kind)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            kind = null;
            return true;
        }

        if (Enum.TryParse<CatalogItemKind>(value, true, out var parsed) && Enum.IsDefined(parsed))
        {
            kind = parsed;
            return true;
        }

        kind = null;
        return false;
    }

    private static string? NormalizeSearch(string? search) => string.IsNullOrWhiteSpace(search) ? null : search.Trim();

    private static ApplicationError? CheckPrecondition(uint rowVersion, byte[]? expectedVersion)
    {
        if (expectedVersion is null)
            return new ApplicationError("http.precondition-required", "If-Match is required.");
        if (!ConcurrencyVersion.Matches(rowVersion, expectedVersion))
            return new ApplicationError("http.precondition-failed", "Catalog item changed; reload before editing.");
        return null;
    }

    private static ApplicationResult<T> Success<T>(T value) => ApplicationResult<T>.Success(value);

    private static ApplicationResult<T> NotFound<T>() =>
        Failure<T>("catalog.item.not-found", "The requested Catalog item was not found.");

    private static ApplicationResult<T> Conflict<T>(string message) => Failure<T>("catalog.item.conflict", message);

    private static ApplicationResult<T> RequestFailure<T>(string message) => Failure<T>("validation.failed", message);

    private static ApplicationResult<T> ValidationFailure<T>(Dictionary<string, string[]> errors) =>
        ApplicationResult<T>.Failure(new ApplicationError("validation.failed", "One or more validation errors occurred.", errors));

    private static ApplicationResult<T> Failure<T>(string code, string message) =>
        ApplicationResult<T>.Failure(new ApplicationError(code, message));
}
