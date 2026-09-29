using NovaHaven.Application.Common.Concurrency;
using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Features.Notifications;
using NovaHaven.Application.Features.Rewards.Commands;
using NovaHaven.Application.Features.Rewards.Queries;
using NovaHaven.Application.Features.Rewards.Repositories;
using NovaHaven.Application.Features.Rewards.Results;
using NovaHaven.Application.Features.Rewards.Validators;
using NovaHaven.Domain.Rewards.Entities;

namespace NovaHaven.Application.Features.Rewards.Services;

public sealed class RewardService(IRewardRepository repository, IUnitOfWork unitOfWork, IUserNotificationPublisher notifications)
{
    public async Task<ApplicationResult<RewardPageResult>> ListPublishedAsync(RewardListQuery query, CancellationToken cancellationToken)
    {
        var page = query.Page ?? 1;
        var pageSize = query.PageSize ?? 20;
        if (page < 1 || pageSize is < 1 or > 50 || (query.Search?.Length ?? 0) > 100)
            return RequestFailure<RewardPageResult>("Invalid reward pagination or filter.");
        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue) return RequestFailure<RewardPageResult>("Page is out of range.");
        var search = string.IsNullOrWhiteSpace(query.Search) ? null : query.Search.Trim();
        var total = await repository.CountPublishedAsync(search, cancellationToken);
        var items = await repository.ListPublishedAsync(search, (int)offset, pageSize, cancellationToken);
        return Success(new RewardPageResult(items, page, pageSize, total));
    }

    public async Task<ApplicationResult<RewardDetailResult>> GetPublishedAsync(string slug, CancellationToken cancellationToken)
    {
        var item = await repository.FindPublishedBySlugAsync(slug, cancellationToken);
        return item is null ? NotFound<RewardDetailResult>() : Success(item);
    }

    public async Task<ApplicationResult<IReadOnlyList<RewardAdminListItemResult>>> ListAdminAsync(CancellationToken cancellationToken) =>
        Success(await repository.ListAdminAsync(cancellationToken));

    public async Task<ApplicationResult<RewardAdminDraftResult>> GetAdminDraftAsync(Guid id, CancellationToken cancellationToken)
    {
        var item = await repository.FindAdminDraftAsync(id, cancellationToken);
        return item is null ? NotFound<RewardAdminDraftResult>() : Success(item);
    }

    public async Task<ApplicationResult<RewardWriteResult>> CreateAsync(RewardInput input, Guid? actorId, CancellationToken cancellationToken)
    {
        var errors = RewardValidator.Validate(input);
        if (errors.Count > 0) return ValidationFailure<RewardWriteResult>(errors);
        if (await repository.IsSlugInUseAsync(input.Slug, null, cancellationToken))
            return Conflict<RewardWriteResult>("Reward slug already exists.");
        Enum.TryParse<RewardDefinitionKind>(input.Kind, true, out var kind);
        var now = DateTimeOffset.UtcNow;
        var definition = new RewardDefinition
        {
            Slug = input.Slug, DraftName = input.Name.Trim(), DraftSummary = input.Summary,
            DraftMarkdown = input.Markdown, DraftKind = kind,
            DraftDeliveryDescription = input.DeliveryDescription.Trim(),
            ExternalAcknowledgementRequired = true, CreatedAt = now, UpdatedAt = now
        };
        repository.Add(definition);
        repository.AddAudit(actorId, "reward.created", definition.Id, new { definition.Slug });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(new RewardWriteResult(definition.Id, definition.Slug, definition.RowVersion));
    }

    public async Task<ApplicationResult<RewardWriteResult>> UpdateAsync(
        Guid id, RewardInput input, byte[]? expectedVersion, Guid? actorId, CancellationToken cancellationToken)
    {
        var definition = await repository.FindForUpdateAsync(id, cancellationToken);
        if (definition is null) return NotFound<RewardWriteResult>();
        var precondition = CheckPrecondition(definition.RowVersion, expectedVersion);
        if (precondition is not null) return ApplicationResult<RewardWriteResult>.Failure(precondition);
        var errors = RewardValidator.Validate(input);
        if (errors.Count > 0) return ValidationFailure<RewardWriteResult>(errors);
        if (input.Slug != definition.Slug && await repository.IsSlugInUseAsync(input.Slug, id, cancellationToken))
            return Conflict<RewardWriteResult>("Reward slug already exists.");
        Enum.TryParse<RewardDefinitionKind>(input.Kind, true, out var kind);
        definition.Slug = input.Slug;
        definition.DraftName = input.Name.Trim();
        definition.DraftSummary = input.Summary;
        definition.DraftMarkdown = input.Markdown;
        definition.DraftKind = kind;
        definition.DraftDeliveryDescription = input.DeliveryDescription.Trim();
        definition.UpdatedAt = DateTimeOffset.UtcNow;
        repository.AddAudit(actorId, "reward.edited", definition.Id, new { definition.Slug });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(new RewardWriteResult(definition.Id, definition.Slug, definition.RowVersion));
    }

    public async Task<ApplicationResult<RewardPublishResult>> PublishAsync(
        Guid id, byte[]? expectedVersion, Guid publisherId, Guid? actorId, CancellationToken cancellationToken)
    {
        var definition = await repository.FindForUpdateAsync(id, cancellationToken);
        if (definition is null) return NotFound<RewardPublishResult>();
        var precondition = CheckPrecondition(definition.RowVersion, expectedVersion);
        if (precondition is not null) return ApplicationResult<RewardPublishResult>.Failure(precondition);
        var revision = RewardDefinitionRevision.FromDraft(definition, publisherId, DateTimeOffset.UtcNow);
        definition.LatestRevisionNumber = revision.Number;
        definition.PublishedRevisionId = revision.Id;
        definition.State = RewardDefinitionState.Published;
        definition.WasPublished = true;
        definition.ExternalAcknowledgementRequired = true;
        definition.UpdatedAt = revision.PublishedAt;
        repository.AddRevision(revision);
        var notification = await notifications.StageForAllUsersAsync(
            $"Phần thưởng mới: {definition.DraftName}", definition.DraftSummary, $"/rewards/{definition.Slug}", cancellationToken);
        repository.AddAudit(actorId, "reward.published", definition.Id,
            new { revision.Number, externalAcknowledgementRequired = true });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        await notifications.DeliverPushAsync(notification, cancellationToken);
        return Success(new RewardPublishResult(definition.Id, revision.Number, definition.RowVersion));
    }

    public async Task<ApplicationResult<uint>> UnpublishAsync(
        Guid id, byte[]? expectedVersion, Guid? actorId, CancellationToken cancellationToken)
    {
        var definition = await repository.FindForUpdateAsync(id, cancellationToken);
        if (definition is null) return NotFound<uint>();
        var precondition = CheckPrecondition(definition.RowVersion, expectedVersion);
        if (precondition is not null) return ApplicationResult<uint>.Failure(precondition);
        if (definition.State != RewardDefinitionState.Published)
            return Conflict<uint>("Reward is not currently published.");
        definition.State = RewardDefinitionState.Unpublished;
        definition.UpdatedAt = DateTimeOffset.UtcNow;
        repository.AddAudit(actorId, "reward.unpublished", definition.Id);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(definition.RowVersion);
    }

    private static ApplicationError? CheckPrecondition(uint rowVersion, byte[]? expectedVersion)
    {
        if (expectedVersion is null) return new ApplicationError("http.precondition-required", "If-Match is required.");
        if (!ConcurrencyVersion.Matches(rowVersion, expectedVersion))
            return new ApplicationError("http.precondition-failed", "Reward definition changed; reload before editing.");
        return null;
    }
    private static ApplicationResult<T> Success<T>(T value) => ApplicationResult<T>.Success(value);
    private static ApplicationResult<T> NotFound<T>() => Failure<T>("reward.definition.not-found", "The requested Reward definition was not found.");
    private static ApplicationResult<T> Conflict<T>(string message) => Failure<T>("reward.definition.conflict", message);
    private static ApplicationResult<T> RequestFailure<T>(string message) => Failure<T>("validation.failed", message);
    private static ApplicationResult<T> ValidationFailure<T>(Dictionary<string, string[]> errors) =>
        ApplicationResult<T>.Failure(new ApplicationError("validation.failed", "One or more validation errors occurred.", errors));
    private static ApplicationResult<T> Failure<T>(string code, string message) =>
        ApplicationResult<T>.Failure(new ApplicationError(code, message));
}
