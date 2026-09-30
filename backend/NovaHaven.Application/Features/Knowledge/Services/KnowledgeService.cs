using NovaHaven.Application.Common.Concurrency;
using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Features.Knowledge.Commands;
using NovaHaven.Application.Features.Knowledge.Repositories;
using NovaHaven.Application.Features.Knowledge.Results;
using NovaHaven.Application.Features.Notifications;
using NovaHaven.Application.Knowledge;
using NovaHaven.Domain.Knowledge;
using NovaHaven.Domain.Knowledge.Entities;

namespace NovaHaven.Application.Features.Knowledge.Services;

public sealed class KnowledgeService(
    IKnowledgeRepository repository,
    IUnitOfWork unitOfWork,
    IUserNotificationPublisher notifications)
{
    public async Task<ApplicationResult<KnowledgePageResult>> ListPublishedAsync(
        string? kindValue, string? search, int? requestedPage, int? requestedPageSize,
        CancellationToken cancellationToken)
    {
        if (!TryParseKind(kindValue, allowAll: true, out var kind))
            return InvalidKind<KnowledgePageResult>();

        var page = requestedPage ?? 1;
        var pageSize = requestedPageSize ?? 20;
        if (page < 1 || pageSize is < 1 or > 50 || (search?.Length ?? 0) > 100)
            return RequestFailure<KnowledgePageResult>("Invalid knowledge pagination or filter.");

        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue)
            return RequestFailure<KnowledgePageResult>("Page is out of range.");

        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var total = await repository.CountPublishedAsync(kind, normalizedSearch, cancellationToken);
        var items = await repository.ListPublishedAsync(kind, normalizedSearch, (int)offset, pageSize, cancellationToken);
        return Success(new KnowledgePageResult(items, page, pageSize, total));
    }

    public async Task<ApplicationResult<KnowledgePublicDetailResult>> GetPublishedAsync(
        string kindValue, string slug, CancellationToken cancellationToken)
    {
        if (!TryParseKind(kindValue, allowAll: false, out var kind))
            return InvalidKind<KnowledgePublicDetailResult>();

        var result = await repository.FindPublishedAsync(kind!.Value, slug, cancellationToken);
        return result is null ? NotFound<KnowledgePublicDetailResult>() : Success(result);
    }

    public async Task<ApplicationResult<IReadOnlyList<KnowledgeAdminListItemResult>>> ListAdminAsync(
        string kindValue, CancellationToken cancellationToken)
    {
        if (!TryParseKind(kindValue, allowAll: false, out var kind))
            return InvalidKind<IReadOnlyList<KnowledgeAdminListItemResult>>();
        return Success(await repository.ListAdminAsync(kind!.Value, cancellationToken));
    }

    public async Task<ApplicationResult<KnowledgeAdminDraftResult>> GetAdminAsync(
        string kindValue, Guid id, CancellationToken cancellationToken)
    {
        if (!TryParseKind(kindValue, allowAll: false, out var kind))
            return InvalidKind<KnowledgeAdminDraftResult>();
        var result = await repository.FindAdminAsync(kind!.Value, id, cancellationToken);
        return result is null ? NotFound<KnowledgeAdminDraftResult>() : Success(result);
    }

    public async Task<ApplicationResult<KnowledgeWriteResult>> CreateAsync(
        string kindValue, KnowledgeDraftCommand command, Guid? actorId, CancellationToken cancellationToken)
    {
        if (!TryBuild(kindValue, command, out var parsed, out var error))
            return ApplicationResult<KnowledgeWriteResult>.Failure(error!);

        var references = await repository.ValidateDraftReferencesAsync(
            parsed!.Common, parsed.Npc, parsed.Quest, parsed.Links, null, cancellationToken);
        if (references.Count > 0) return ValidationFailure<KnowledgeWriteResult>(references);
        if (await repository.IsSlugInUseAsync(parsed.Common.Slug, null, cancellationToken))
            return Conflict<KnowledgeWriteResult>("Knowledge slug already exists.");

        var now = DateTimeOffset.UtcNow;
        var entry = new GameKnowledgeEntry
        {
            Slug = parsed.Common.Slug.Trim(),
            DraftName = parsed.Common.Name.Trim(),
            DraftSummary = parsed.Common.Summary.Trim(),
            DraftMarkdown = parsed.Common.Markdown,
            Kind = parsed.Common.Kind,
            CreatedAt = now,
            UpdatedAt = now
        };
        repository.Add(entry);
        repository.AddDraftData(entry, parsed.Npc, parsed.Quest, parsed.Location, parsed.Season, parsed.Links);
        repository.AddAudit(actorId, "knowledge.created", entry.Id, new { entry.Kind, entry.Slug });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(new KnowledgeWriteResult(entry.Id, entry.Slug, entry.RowVersion));
    }

    public async Task<ApplicationResult<KnowledgeWriteResult>> UpdateAsync(
        string kindValue, Guid id, KnowledgeDraftCommand command, byte[]? expectedVersion,
        Guid? actorId, CancellationToken cancellationToken)
    {
        if (!TryParseKind(kindValue, allowAll: false, out var kind))
            return InvalidKind<KnowledgeWriteResult>();
        var entry = await repository.FindForUpdateAsync(kind!.Value, id, cancellationToken);
        if (entry is null) return NotFound<KnowledgeWriteResult>();
        var precondition = CheckPrecondition(entry.RowVersion, expectedVersion);
        if (precondition is not null) return ApplicationResult<KnowledgeWriteResult>.Failure(precondition);
        if (!TryBuild(kindValue, command, out var parsed, out var error))
            return ApplicationResult<KnowledgeWriteResult>.Failure(error!);

        var references = await repository.ValidateDraftReferencesAsync(
            parsed!.Common, parsed.Npc, parsed.Quest, parsed.Links, id, cancellationToken);
        if (references.Count > 0) return ValidationFailure<KnowledgeWriteResult>(references);
        var slug = parsed.Common.Slug.Trim();
        if (slug != entry.Slug && await repository.IsSlugInUseAsync(slug, id, cancellationToken))
            return Conflict<KnowledgeWriteResult>("Knowledge slug already exists.");

        entry.Slug = slug;
        entry.DraftName = parsed.Common.Name.Trim();
        entry.DraftSummary = parsed.Common.Summary.Trim();
        entry.DraftMarkdown = parsed.Common.Markdown;
        entry.UpdatedAt = DateTimeOffset.UtcNow;
        await repository.ReplaceDraftDataAsync(
            entry.Id, kind.Value, parsed.Npc, parsed.Quest, parsed.Location, parsed.Season, parsed.Links, cancellationToken);
        repository.AddAudit(actorId, "knowledge.edited", entry.Id, new { entry.Kind, entry.Slug });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(new KnowledgeWriteResult(entry.Id, entry.Slug, entry.RowVersion));
    }

    public async Task<ApplicationResult<KnowledgePublishResult>> PublishAsync(
        string kindValue, Guid id, byte[]? expectedVersion, Guid? publisherId, Guid? actorId,
        CancellationToken cancellationToken)
    {
        if (!TryParseKind(kindValue, allowAll: false, out var kind))
            return InvalidKind<KnowledgePublishResult>();
        if (publisherId is null)
            return Failure<KnowledgePublishResult>("auth.required", "A signed-in Admin is required to publish.");

        NotificationBatch? notification = null;
        var result = await unitOfWork.ExecuteInTransactionAsync(
            async transactionToken =>
            {
                var entry = await repository.FindForUpdateAsync(kind!.Value, id, transactionToken);
                if (entry is null) return NotFound<KnowledgePublishResult>();
                var precondition = CheckPrecondition(entry.RowVersion, expectedVersion);
                if (precondition is not null)
                    return ApplicationResult<KnowledgePublishResult>.Failure(precondition);

                var validation = await repository.ValidatePublicationAsync(entry, transactionToken);
                if (validation.Count > 0) return ValidationFailure<KnowledgePublishResult>(validation);

                var now = DateTimeOffset.UtcNow;
                var revision = GameKnowledgeRevision.FromDraft(entry, publisherId.Value, now);
                await repository.AddRevisionDataAsync(entry, revision, transactionToken);
                entry.State = KnowledgeState.Published;
                entry.WasPublished = true;
                entry.PublishedRevisionId = revision.Id;
                entry.LatestRevisionNumber = revision.Number;
                entry.UpdatedAt = now;
                notification = await notifications.StageForAllUsersAsync(
                    $"Thế giới cập nhật: {entry.DraftName}", entry.DraftSummary,
                    $"/knowledge/{kindValue}/{entry.Slug}", transactionToken);
                repository.AddAudit(actorId, "knowledge.published", entry.Id,
                    new { entry.Kind, revision = revision.Number });
                await unitOfWork.SaveChangesAsync(transactionToken);
                return Success(new KnowledgePublishResult(entry.Id, revision.Id, revision.Number, entry.RowVersion));
            },
            operationResult => operationResult.IsSuccess,
            TransactionIsolation.Serializable,
            cancellationToken);

        if (result.IsSuccess && notification is not null)
            await notifications.DeliverPushAsync(notification, cancellationToken);
        return result;
    }

    public async Task<ApplicationResult<uint>> UnpublishAsync(
        string kindValue, Guid id, byte[]? expectedVersion, Guid? actorId, CancellationToken cancellationToken)
    {
        if (!TryParseKind(kindValue, allowAll: false, out var kind))
            return InvalidKind<uint>();
        var entry = await repository.FindForUpdateAsync(kind!.Value, id, cancellationToken);
        if (entry is null) return NotFound<uint>();
        var precondition = CheckPrecondition(entry.RowVersion, expectedVersion);
        if (precondition is not null) return ApplicationResult<uint>.Failure(precondition);
        if (entry.State != KnowledgeState.Published)
            return Conflict<uint>("Knowledge entry is not currently published.");

        entry.State = KnowledgeState.Unpublished;
        entry.UpdatedAt = DateTimeOffset.UtcNow;
        repository.AddAudit(actorId, "knowledge.unpublished", entry.Id, new { entry.Kind });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(entry.RowVersion);
    }

    private static bool TryBuild(
        string routeKind,
        KnowledgeDraftCommand command,
        out ParsedKnowledge? parsed,
        out ApplicationError? error)
    {
        parsed = null;
        error = null;
        if (!TryParseKind(routeKind, allowAll: false, out var kind))
        {
            error = KindError();
            return false;
        }
        if (!string.IsNullOrWhiteSpace(command.BodyKind)
            && (!Enum.TryParse<KnowledgeKind>(command.BodyKind, true, out var bodyKind) || bodyKind != kind))
        {
            error = ValidationError(new Dictionary<string, string[]>
                { ["kind"] = ["Kind does not match the route."] });
            return false;
        }

        var common = new KnowledgeCommonInput(
            command.Name ?? "", command.Slug ?? "", command.Summary ?? "", command.Markdown ?? "", kind!.Value);
        var errors = KnowledgeValidator.ValidateCommon(common);
        NpcInput? npc = null;
        QuestInput? quest = null;
        LocationInput? location = null;
        SeasonInput? season = null;
        switch (kind.Value)
        {
            case KnowledgeKind.Npc:
                npc = new NpcInput(command.Role ?? "", command.LocationEntryId, command.PortraitUrl);
                Merge(errors, KnowledgeValidator.ValidateNpc(npc));
                break;
            case KnowledgeKind.Quest:
                quest = new QuestInput(command.Difficulty ?? 0, command.GiverNpcEntryId, command.LocationEntryId,
                    command.RewardDescription ?? "", command.Steps ?? []);
                Merge(errors, KnowledgeValidator.ValidateQuest(quest));
                break;
            case KnowledgeKind.Location:
                location = new LocationInput(command.Region ?? "", command.LocationType ?? "",
                    command.Latitude, command.Longitude, command.MapImageUrl);
                Merge(errors, KnowledgeValidator.ValidateLocation(location));
                break;
            case KnowledgeKind.Season:
                season = new SeasonInput(command.StartsAt ?? default, command.EndsAt ?? default,
                    command.Theme ?? "", command.EventDescription);
                Merge(errors, KnowledgeValidator.ValidateSeason(season));
                break;
        }

        var links = command.Links ?? [];
        Merge(errors, KnowledgeValidator.ValidateLinks(links));
        if (errors.Count > 0)
        {
            error = ValidationError(errors);
            return false;
        }
        parsed = new ParsedKnowledge(common, npc, quest, location, season, links);
        return true;
    }

    private static bool TryParseKind(string? value, bool allowAll, out KnowledgeKind? kind)
    {
        if (allowAll && string.IsNullOrWhiteSpace(value))
        {
            kind = null;
            return true;
        }
        if (Enum.TryParse<KnowledgeKind>(value, true, out var parsed) && Enum.IsDefined(parsed))
        {
            kind = parsed;
            return true;
        }
        kind = null;
        return false;
    }

    private static ApplicationError? CheckPrecondition(uint rowVersion, byte[]? expectedVersion)
    {
        if (expectedVersion is null)
            return new ApplicationError("http.precondition-required", "If-Match is required.");
        if (!ConcurrencyVersion.Matches(rowVersion, expectedVersion))
            return new ApplicationError("http.precondition-failed", "Knowledge entry changed; reload before editing.");
        return null;
    }

    private static void Merge(Dictionary<string, string[]> target, Dictionary<string, string[]> source)
    {
        foreach (var pair in source) target[pair.Key] = pair.Value;
    }

    private static ApplicationError KindError() =>
        new("knowledge.kind.invalid", "Knowledge kind is not supported.");

    private static ApplicationError ValidationError(Dictionary<string, string[]> errors) =>
        new("validation.failed", "One or more validation errors occurred.", errors);

    private static ApplicationResult<T> InvalidKind<T>() => ApplicationResult<T>.Failure(KindError());

    private static ApplicationResult<T> ValidationFailure<T>(Dictionary<string, string[]> errors) =>
        ApplicationResult<T>.Failure(ValidationError(errors));

    private static ApplicationResult<T> RequestFailure<T>(string message) =>
        ApplicationResult<T>.Failure(new ApplicationError("validation.failed", message));

    private static ApplicationResult<T> NotFound<T>() =>
        Failure<T>("knowledge.entry.not-found", "The requested Knowledge entry was not found.");

    private static ApplicationResult<T> Conflict<T>(string message) =>
        Failure<T>("knowledge.entry.conflict", message);

    private static ApplicationResult<T> Failure<T>(string code, string message) =>
        ApplicationResult<T>.Failure(new ApplicationError(code, message));

    private static ApplicationResult<T> Success<T>(T value) => ApplicationResult<T>.Success(value);

    private sealed record ParsedKnowledge(
        KnowledgeCommonInput Common,
        NpcInput? Npc,
        QuestInput? Quest,
        LocationInput? Location,
        SeasonInput? Season,
        IReadOnlyList<KnowledgeLinkInput> Links);
}
