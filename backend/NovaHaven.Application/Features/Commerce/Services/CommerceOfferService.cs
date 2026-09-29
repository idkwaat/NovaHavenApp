using NovaHaven.Application.Common.Concurrency;
using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Commerce;
using NovaHaven.Application.Features.Commerce.Commands;
using NovaHaven.Application.Features.Commerce.Repositories;
using NovaHaven.Application.Features.Commerce.Results;
using NovaHaven.Application.Features.Notifications;
using NovaHaven.Domain.Commerce;
using NovaHaven.Domain.Commerce.Entities;

namespace NovaHaven.Application.Features.Commerce.Services;

public sealed class CommerceOfferService(
    ICommerceOfferRepository repository,
    IUnitOfWork unitOfWork,
    IUserNotificationPublisher notifications)
{
    public async Task<ApplicationResult<CommerceOfferPageResult>> ListPublishedAsync(
        string? search, int? requestedPage, int? requestedPageSize, CancellationToken cancellationToken)
    {
        var page = requestedPage ?? 1;
        var pageSize = requestedPageSize ?? 20;
        if (page < 1 || pageSize is < 1 or > 50 || (search?.Length ?? 0) > 100)
            return Invalid<CommerceOfferPageResult>("Invalid commerce pagination or filter.");
        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue) return Invalid<CommerceOfferPageResult>("Page is out of range.");
        var term = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var total = await repository.CountPublishedAsync(term, cancellationToken);
        var items = await repository.ListPublishedAsync(term, (int)offset, pageSize, cancellationToken);
        return Success(new CommerceOfferPageResult(items, page, pageSize, total));
    }

    public async Task<ApplicationResult<CommerceOfferDetailResult>> GetPublishedAsync(
        string slug, CancellationToken cancellationToken)
    {
        var result = await repository.FindPublishedAsync(slug, cancellationToken);
        return result is null ? Failure<CommerceOfferDetailResult>("commerce.offer.not-found", "Commerce offer was not found.") : Success(result);
    }

    public async Task<IReadOnlyList<CommerceOfferAdminListResult>> ListAdminAsync(CancellationToken cancellationToken) =>
        await repository.ListAdminAsync(cancellationToken);

    public async Task<ApplicationResult<CommerceOfferAdminDraftResult>> GetAdminAsync(Guid id, CancellationToken cancellationToken)
    {
        var result = await repository.FindAdminAsync(id, cancellationToken);
        return result is null ? Failure<CommerceOfferAdminDraftResult>("commerce.offer.not-found", "Commerce offer was not found.") : Success(result);
    }

    public async Task<ApplicationResult<CommerceOfferWriteResult>> CreateAsync(
        CommerceOfferCommand command, Guid? actorId, CancellationToken cancellationToken)
    {
        if (!TryBuild(command, out var input, out var kind, out var errors)) return ValidationFailure<CommerceOfferWriteResult>(errors);
        if (await repository.IsSlugInUseAsync(input!.Slug, null, cancellationToken))
            return Conflict<CommerceOfferWriteResult>("Commerce offer slug already exists.");
        var offer = new CommerceOffer
        {
            Slug = input.Slug, DraftName = input.Name.Trim(), DraftSummary = input.Summary,
            DraftMarkdown = input.Markdown, DraftKind = kind, DraftDisplayPrice = input.DisplayPrice.Trim(),
            DraftProviderProductCode = input.ProviderProductCode?.Trim(), DraftIsPurchasable = input.IsPurchasable,
            DraftPriceMinorUnits = input.PriceMinorUnits, UpdatedAt = DateTimeOffset.UtcNow
        };
        repository.Add(offer);
        repository.AddAudit(actorId, "commerce.offer.created", offer.Id,
            new { offer.Slug, definitionOnly = true, offer.DraftIsPurchasable, offer.DraftPriceMinorUnits });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(new CommerceOfferWriteResult(offer.Id, offer.Slug, offer.LatestRevisionNumber, offer.RowVersion));
    }

    public async Task<ApplicationResult<CommerceOfferWriteResult>> UpdateAsync(
        Guid id, CommerceOfferCommand command, byte[]? expectedVersion, Guid? actorId, CancellationToken cancellationToken)
    {
        var offer = await repository.FindForUpdateAsync(id, cancellationToken);
        if (offer is null) return Failure<CommerceOfferWriteResult>("commerce.offer.not-found", "Commerce offer was not found.");
        var precondition = CheckPrecondition(offer.RowVersion, expectedVersion);
        if (precondition is not null) return ApplicationResult<CommerceOfferWriteResult>.Failure(precondition);
        if (!TryBuild(command, out var input, out var kind, out var errors)) return ValidationFailure<CommerceOfferWriteResult>(errors);
        if (input!.Slug != offer.Slug && await repository.IsSlugInUseAsync(input.Slug, id, cancellationToken))
            return Conflict<CommerceOfferWriteResult>("Commerce offer slug already exists.");
        offer.Slug = input.Slug;
        offer.DraftName = input.Name.Trim();
        offer.DraftSummary = input.Summary;
        offer.DraftMarkdown = input.Markdown;
        offer.DraftKind = kind;
        offer.DraftDisplayPrice = input.DisplayPrice.Trim();
        offer.DraftProviderProductCode = input.ProviderProductCode?.Trim();
        offer.DraftIsPurchasable = input.IsPurchasable;
        offer.DraftPriceMinorUnits = input.PriceMinorUnits;
        offer.UpdatedAt = DateTimeOffset.UtcNow;
        repository.AddAudit(actorId, "commerce.offer.edited", offer.Id,
            new { offer.Slug, definitionOnly = true, offer.DraftIsPurchasable, offer.DraftPriceMinorUnits });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(new CommerceOfferWriteResult(offer.Id, offer.Slug, offer.LatestRevisionNumber, offer.RowVersion));
    }

    public async Task<ApplicationResult<CommerceOfferWriteResult>> PublishAsync(
        Guid id, byte[]? expectedVersion, Guid? publisherId, Guid? actorId, CancellationToken cancellationToken)
    {
        if (publisherId is null) return Failure<CommerceOfferWriteResult>("auth.required", "A signed-in Admin is required to publish.");
        NotificationBatch? notification = null;
        var result = await unitOfWork.ExecuteInTransactionAsync(
            async token =>
            {
                var offer = await repository.FindForUpdateAsync(id, token);
                if (offer is null) return Failure<CommerceOfferWriteResult>("commerce.offer.not-found", "Commerce offer was not found.");
                var precondition = CheckPrecondition(offer.RowVersion, expectedVersion);
                if (precondition is not null) return ApplicationResult<CommerceOfferWriteResult>.Failure(precondition);
                if (await repository.IsPublishedSlugInUseAsync(offer.Slug, id, token))
                    return Conflict<CommerceOfferWriteResult>("A published commerce offer already uses this slug.");
                var revision = CommerceOfferRevision.FromDraft(offer, publisherId.Value, DateTimeOffset.UtcNow);
                repository.AddRevision(revision);
                offer.LatestRevisionNumber = revision.Number;
                offer.PublishedRevisionId = revision.Id;
                offer.State = CommerceOfferState.Published;
                offer.WasPublished = true;
                offer.UpdatedAt = revision.PublishedAt;
                repository.AddAudit(actorId, "commerce.offer.published", offer.Id,
                    new { revision.Number, definitionOnly = true });
                notification = await notifications.StageForAllUsersAsync(
                    $"Sản phẩm mới: {offer.DraftName}", offer.DraftSummary, $"/commerce/{offer.Slug}", token);
                await unitOfWork.SaveChangesAsync(token);
                return Success(new CommerceOfferWriteResult(offer.Id, offer.Slug, revision.Number, offer.RowVersion));
            }, operation => operation.IsSuccess, TransactionIsolation.Serializable, cancellationToken);
        if (result.IsSuccess && notification is not null)
            await notifications.DeliverPushAsync(notification, cancellationToken);
        return result;
    }

    public async Task<ApplicationResult<uint>> UnpublishAsync(
        Guid id, byte[]? expectedVersion, Guid? actorId, CancellationToken cancellationToken)
    {
        var offer = await repository.FindForUpdateAsync(id, cancellationToken);
        if (offer is null) return Failure<uint>("commerce.offer.not-found", "Commerce offer was not found.");
        var precondition = CheckPrecondition(offer.RowVersion, expectedVersion);
        if (precondition is not null) return ApplicationResult<uint>.Failure(precondition);
        if (offer.State != CommerceOfferState.Published) return Conflict<uint>("Commerce offer is not currently published.");
        offer.State = CommerceOfferState.Unpublished;
        offer.UpdatedAt = DateTimeOffset.UtcNow;
        repository.AddAudit(actorId, "commerce.offer.unpublished", offer.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(offer.RowVersion);
    }

    private static bool TryBuild(CommerceOfferCommand command, out CommerceInput? input,
        out CommerceOfferKind kind, out Dictionary<string, string[]> errors)
    {
        input = new CommerceInput(command.Name ?? "", command.Slug ?? "", command.Summary ?? "",
            command.Markdown ?? "", command.Kind ?? "", command.DisplayPrice ?? "",
            command.ProviderProductCode, command.IsPurchasable, command.PriceMinorUnits);
        errors = CommerceValidator.Validate(input);
        kind = Enum.TryParse<CommerceOfferKind>(input.Kind, true, out var parsed) && Enum.IsDefined(parsed)
            ? parsed : CommerceOfferKind.Other;
        return errors.Count == 0;
    }

    private static ApplicationError? CheckPrecondition(uint current, byte[]? expected) =>
        expected is null ? new ApplicationError("http.precondition-required", "If-Match is required.")
            : !ConcurrencyVersion.Matches(current, expected)
                ? new ApplicationError("http.precondition-failed", "Commerce offer changed; reload before editing.") : null;

    private static ApplicationResult<T> Invalid<T>(string message) => ApplicationResult<T>.Failure(new ApplicationError("validation.failed", message));
    private static ApplicationResult<T> ValidationFailure<T>(Dictionary<string, string[]> errors) =>
        ApplicationResult<T>.Failure(new ApplicationError("validation.failed", "One or more validation errors occurred.", errors));
    private static ApplicationResult<T> Conflict<T>(string message) => Failure<T>("commerce.offer.conflict", message);
    private static ApplicationResult<T> Failure<T>(string code, string message) => ApplicationResult<T>.Failure(new ApplicationError(code, message));
    private static ApplicationResult<T> Success<T>(T value) => ApplicationResult<T>.Success(value);
}
