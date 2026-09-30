using NovaHaven.Application.Common.Concurrency;
using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Community;
using NovaHaven.Application.Features.Community.Commands;
using NovaHaven.Application.Features.Community.Repositories;
using NovaHaven.Application.Features.Community.Results;
using NovaHaven.Application.Features.Notifications;
using NovaHaven.Domain.Community;
using NovaHaven.Domain.Community.Entities;
using NovaHaven.Domain.Knowledge;

namespace NovaHaven.Application.Features.Community.Services;

public sealed class CommunityService(
    ICommunityRepository repository,
    IUnitOfWork unitOfWork,
    IUserNotificationPublisher notifications)
{
    public async Task<ApplicationResult<CommunityPageResult>> ListPublishedAsync(
        string? kindValue, string? search, int? requestedPage, int? requestedPageSize,
        CancellationToken cancellationToken)
    {
        if (!TryParseKind(kindValue, true, out var kind)) return KindFailure<CommunityPageResult>();
        var page = requestedPage ?? 1;
        var pageSize = requestedPageSize ?? 20;
        if (page < 1 || pageSize is < 1 or > 50 || (search?.Length ?? 0) > 100)
            return Invalid<CommunityPageResult>("Invalid community pagination or filter.");
        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue) return Invalid<CommunityPageResult>("Page is out of range.");
        var normalizedSearch = string.IsNullOrWhiteSpace(search) ? null : search.Trim();
        var total = await repository.CountPublishedAsync(kind, normalizedSearch, cancellationToken);
        var items = await repository.ListPublishedAsync(kind, normalizedSearch, (int)offset, pageSize, cancellationToken);
        return Success(new CommunityPageResult(items, page, pageSize, total));
    }

    public async Task<ApplicationResult<CommunityPublicDetailResult>> GetPublishedAsync(
        string kindValue, string slug, CancellationToken cancellationToken)
    {
        if (!TryParseKind(kindValue, false, out var kind)) return KindFailure<CommunityPublicDetailResult>();
        var result = await repository.FindPublishedAsync(kind!.Value, slug, cancellationToken);
        return result is null ? Failure<CommunityPublicDetailResult>("community.record.not-found", "Community record was not found.") : Success(result);
    }

    public async Task<ApplicationResult<IReadOnlyList<CommunityAdminListItemResult>>> ListAdminAsync(
        string kindValue, CancellationToken cancellationToken)
    {
        if (!TryParseKind(kindValue, false, out var kind)) return KindFailure<IReadOnlyList<CommunityAdminListItemResult>>();
        return Success(await repository.ListAdminAsync(kind!.Value, cancellationToken));
    }

    public async Task<ApplicationResult<CommunityAdminDraftResult>> GetAdminAsync(
        string kindValue, Guid id, CancellationToken cancellationToken)
    {
        if (!TryParseKind(kindValue, false, out var kind)) return KindFailure<CommunityAdminDraftResult>();
        var result = await repository.FindAdminAsync(kind!.Value, id, cancellationToken);
        return result is null ? Failure<CommunityAdminDraftResult>("community.record.not-found", "Community record was not found.") : Success(result);
    }

    public async Task<ApplicationResult<CommunityWriteResult>> CreateAsync(
        string kindValue, CommunityDraftCommand command, Guid? actorId, CancellationToken cancellationToken)
    {
        if (!TryBuild(kindValue, command, out var parsed, out var error)) return ApplicationResult<CommunityWriteResult>.Failure(error!);
        var references = await repository.ValidateDraftReferencesAsync(
            parsed!.Event?.LocationEntryId ?? parsed.Housing?.LocationEntryId,
            parsed.Leaderboard?.SeasonEntryId, cancellationToken);
        if (references.Count > 0) return ValidationFailure<CommunityWriteResult>(references);
        if (await repository.IsSlugInUseAsync(parsed.Common.Slug, null, cancellationToken))
            return Conflict<CommunityWriteResult>("Community slug already exists.");

        var record = new CommunityRecord
        {
            Slug = parsed.Common.Slug.Trim(), DraftName = parsed.Common.Name.Trim(),
            DraftSummary = parsed.Common.Summary.Trim(), DraftMarkdown = parsed.Common.Markdown,
            Kind = parsed.Common.Kind, UpdatedAt = DateTimeOffset.UtcNow
        };
        ApplyDraft(record, parsed);
        repository.Add(record);
        repository.ReplaceLeaderboardDraftRows(record.Id, parsed.Leaderboard?.Rows ?? []);
        repository.AddAudit(actorId, "community.created", record.Id, new { record.Kind, record.Slug });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(new CommunityWriteResult(record.Id, record.Slug, record.RowVersion));
    }

    public async Task<ApplicationResult<CommunityWriteResult>> UpdateAsync(
        string kindValue, Guid id, CommunityDraftCommand command, byte[]? expectedVersion,
        Guid? actorId, CancellationToken cancellationToken)
    {
        if (!TryParseKind(kindValue, false, out var kind)) return KindFailure<CommunityWriteResult>();
        var record = await repository.FindForUpdateAsync(kind!.Value, id, cancellationToken);
        if (record is null) return Failure<CommunityWriteResult>("community.record.not-found", "Community record was not found.");
        var precondition = CheckPrecondition(record.RowVersion, expectedVersion);
        if (precondition is not null) return ApplicationResult<CommunityWriteResult>.Failure(precondition);
        if (!TryBuild(kindValue, command, out var parsed, out var error)) return ApplicationResult<CommunityWriteResult>.Failure(error!);
        var references = await repository.ValidateDraftReferencesAsync(
            parsed!.Event?.LocationEntryId ?? parsed.Housing?.LocationEntryId,
            parsed.Leaderboard?.SeasonEntryId, cancellationToken);
        if (references.Count > 0) return ValidationFailure<CommunityWriteResult>(references);
        var slug = parsed.Common.Slug.Trim();
        if (slug != record.Slug && await repository.IsSlugInUseAsync(slug, id, cancellationToken))
            return Conflict<CommunityWriteResult>("Community slug already exists.");

        record.Slug = slug;
        record.DraftName = parsed.Common.Name.Trim();
        record.DraftSummary = parsed.Common.Summary.Trim();
        record.DraftMarkdown = parsed.Common.Markdown;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        ApplyDraft(record, parsed);
        repository.ReplaceLeaderboardDraftRows(id, parsed.Leaderboard?.Rows ?? []);
        repository.AddAudit(actorId, "community.edited", id, new { record.Kind, record.Slug });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(new CommunityWriteResult(record.Id, record.Slug, record.RowVersion));
    }

    public async Task<ApplicationResult<CommunityPublishResult>> PublishAsync(
        string kindValue, Guid id, byte[]? expectedVersion, Guid? publisherId, Guid? actorId,
        CancellationToken cancellationToken)
    {
        if (!TryParseKind(kindValue, false, out var kind)) return KindFailure<CommunityPublishResult>();
        if (publisherId is null) return Failure<CommunityPublishResult>("auth.required", "A signed-in Admin is required to publish.");
        NotificationBatch? notification = null;
        var result = await unitOfWork.ExecuteInTransactionAsync(
            async transactionToken =>
            {
                var record = await repository.FindForUpdateAsync(kind!.Value, id, transactionToken);
                if (record is null) return Failure<CommunityPublishResult>("community.record.not-found", "Community record was not found.");
                var precondition = CheckPrecondition(record.RowVersion, expectedVersion);
                if (precondition is not null) return ApplicationResult<CommunityPublishResult>.Failure(precondition);
                var validation = await repository.ValidatePublicationAsync(record, transactionToken);
                if (validation.Count > 0) return ValidationFailure<CommunityPublishResult>(validation);

                var revision = CommunityRecordRevision.FromDraft(record, publisherId.Value, DateTimeOffset.UtcNow);
                revision.LocationRevisionId = record.DraftLocationEntryId is Guid location
                    ? await repository.FindPublishedKnowledgeRevisionIdAsync(location, KnowledgeKind.Location, transactionToken) : null;
                revision.SeasonRevisionId = record.DraftSeasonEntryId is Guid season
                    ? await repository.FindPublishedKnowledgeRevisionIdAsync(season, KnowledgeKind.Season, transactionToken) : null;
                repository.AddRevision(revision);
                if (record.Kind == CommunityKind.Leaderboard)
                    await repository.CopyLeaderboardRowsToRevisionAsync(record.Id, revision.Id, transactionToken);
                record.State = CommunityState.Published;
                record.WasPublished = true;
                record.PublishedRevisionId = revision.Id;
                record.LatestRevisionNumber = revision.Number;
                record.UpdatedAt = revision.PublishedAt;
                notification = await notifications.StageForAllUsersAsync(
                    $"Cộng đồng: {record.DraftName}", record.DraftSummary,
                    $"/community/{kindValue}/{record.Slug}", transactionToken);
                repository.AddAudit(actorId, "community.published", id, new { record.Kind, revision = revision.Number });
                await unitOfWork.SaveChangesAsync(transactionToken);
                return Success(new CommunityPublishResult(record.Id, revision.Id, revision.Number, record.RowVersion));
            }, result => result.IsSuccess, TransactionIsolation.Serializable, cancellationToken);
        if (result.IsSuccess && notification is not null)
            await notifications.DeliverPushAsync(notification, cancellationToken);
        return result;
    }

    public async Task<ApplicationResult<uint>> UnpublishAsync(
        string kindValue, Guid id, byte[]? expectedVersion, Guid? actorId, CancellationToken cancellationToken)
    {
        if (!TryParseKind(kindValue, false, out var kind)) return KindFailure<uint>();
        var record = await repository.FindForUpdateAsync(kind!.Value, id, cancellationToken);
        if (record is null) return Failure<uint>("community.record.not-found", "Community record was not found.");
        var precondition = CheckPrecondition(record.RowVersion, expectedVersion);
        if (precondition is not null) return ApplicationResult<uint>.Failure(precondition);
        if (record.State != CommunityState.Published) return Conflict<uint>("Community record is not currently published.");
        record.State = CommunityState.Unpublished;
        record.UpdatedAt = DateTimeOffset.UtcNow;
        repository.AddAudit(actorId, "community.unpublished", id, new { record.Kind });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(record.RowVersion);
    }

    public async Task<ApplicationResult<IReadOnlyList<CommunityRegistrationResult>>> ListRegistrationsAsync(
        Guid eventId, CancellationToken cancellationToken) =>
        await repository.IsEventAsync(eventId, cancellationToken)
            ? Success(await repository.ListRegistrationsAsync(eventId, cancellationToken))
            : Failure<IReadOnlyList<CommunityRegistrationResult>>("community.event.not-found", "Event was not found.");

    public async Task<ApplicationResult<CommunityRegistrationWriteResult>> AddRegistrationAsync(
        Guid eventId, CommunityRegistrationCommand command, Guid? actorId, CancellationToken cancellationToken)
    {
        if (!await repository.IsEventAsync(eventId, cancellationToken))
            return Failure<CommunityRegistrationWriteResult>("community.event.not-found", "Event was not found.");
        if (string.IsNullOrWhiteSpace(command.DisplayName) || command.DisplayName.Length > 120 || (command.Contact?.Length ?? 0) > 200)
            return ValidationFailure<CommunityRegistrationWriteResult>(new Dictionary<string, string[]>
                { ["displayName"] = ["Display name is required and contact must be at most 200 characters."] });
        var registration = new CommunityEventRegistration
        {
            EventRecordId = eventId, DisplayName = command.DisplayName.Trim(), Contact = command.Contact?.Trim() ?? ""
        };
        repository.AddRegistration(registration);
        repository.AddAudit(actorId, "community.registration.created", registration.Id, new { registration.EventRecordId });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(ToRegistrationWrite(registration));
    }

    public async Task<ApplicationResult<CommunityRegistrationWriteResult>> UpdateRegistrationAsync(
        Guid eventId, Guid registrationId, CommunityRegistrationUpdateCommand command,
        byte[]? expectedVersion, Guid? actorId, CancellationToken cancellationToken)
    {
        var registration = await repository.FindRegistrationForUpdateAsync(eventId, registrationId, cancellationToken);
        if (registration is null) return Failure<CommunityRegistrationWriteResult>("community.registration.not-found", "Event registration was not found.");
        var precondition = CheckPrecondition(registration.RowVersion, expectedVersion, "Registration changed; reload before editing.");
        if (precondition is not null) return ApplicationResult<CommunityRegistrationWriteResult>.Failure(precondition);
        if (!Enum.TryParse<EventRegistrationState>(command.State, true, out var state) || !Enum.IsDefined(state))
            return ValidationFailure<CommunityRegistrationWriteResult>(new Dictionary<string, string[]>
                { ["state"] = ["Unsupported registration state."] });
        registration.State = state;
        repository.AddAudit(actorId, "community.registration.updated", registration.Id, new { state });
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return Success(ToRegistrationWrite(registration));
    }

    private static bool TryBuild(string routeKind, CommunityDraftCommand command,
        out ParsedCommunity? parsed, out ApplicationError? error)
    {
        parsed = null;
        error = null;
        if (!TryParseKind(routeKind, false, out var kind)) { error = KindError(); return false; }
        if (!string.IsNullOrWhiteSpace(command.BodyKind)
            && (!Enum.TryParse<CommunityKind>(command.BodyKind, true, out var bodyKind) || bodyKind != kind))
        {
            error = ValidationError(new Dictionary<string, string[]> { ["kind"] = ["Kind does not match the route."] });
            return false;
        }
        var common = new CommunityCommonInput(command.Name ?? "", command.Slug ?? "", command.Summary ?? "", command.Markdown ?? "", kind!.Value);
        var errors = CommunityValidator.ValidateCommon(common);
        CommunityEventInput? @event = null;
        GuildInput? guild = null;
        PlayerInput? player = null;
        HousingInput? housing = null;
        LeaderboardInput? leaderboard = null;
        switch (kind)
        {
            case CommunityKind.Event:
                @event = new CommunityEventInput(command.StartsAt, command.EndsAt, command.LocationEntryId, command.Capacity, command.RegistrationOpen);
                Merge(errors, CommunityValidator.ValidateEvent(@event)); break;
            case CommunityKind.Guild:
                guild = new GuildInput(command.Motto ?? "", command.DiscordUrl ?? "");
                Merge(errors, CommunityValidator.ValidateGuild(guild)); break;
            case CommunityKind.Player:
                player = new PlayerInput(command.Handle ?? "", command.Bio ?? "", command.AvatarUrl ?? "");
                Merge(errors, CommunityValidator.ValidatePlayer(player)); break;
            case CommunityKind.Housing:
                housing = new HousingInput(command.OwnerDisplayName ?? "", command.GalleryMarkdown ?? "", command.LocationEntryId);
                Merge(errors, CommunityValidator.ValidateHousing(housing)); break;
            case CommunityKind.Leaderboard:
                leaderboard = new LeaderboardInput(command.LeaderboardCategory ?? "", command.Rows ?? [], command.SeasonEntryId);
                Merge(errors, CommunityValidator.ValidateLeaderboard(leaderboard)); break;
        }
        if (errors.Count > 0) { error = ValidationError(errors); return false; }
        parsed = new ParsedCommunity(common, @event, guild, player, housing, leaderboard);
        return true;
    }

    private static void ApplyDraft(CommunityRecord record, ParsedCommunity parsed)
    {
        record.DraftStartsAt = parsed.Event?.StartsAt?.ToUniversalTime();
        record.DraftEndsAt = parsed.Event?.EndsAt?.ToUniversalTime();
        record.DraftLocationEntryId = parsed.Event?.LocationEntryId ?? parsed.Housing?.LocationEntryId;
        record.DraftCapacity = parsed.Event?.Capacity;
        record.DraftRegistrationOpen = parsed.Event?.RegistrationOpen ?? false;
        record.DraftMotto = parsed.Guild?.Motto.Trim() ?? "";
        record.DraftDiscordUrl = parsed.Guild?.DiscordUrl.Trim() ?? "";
        record.DraftHandle = parsed.Player?.Handle.Trim() ?? "";
        record.DraftBio = parsed.Player?.Bio.Trim() ?? "";
        record.DraftAvatarUrl = parsed.Player?.AvatarUrl.Trim() ?? "";
        record.DraftOwnerDisplayName = parsed.Housing?.OwnerDisplayName.Trim() ?? "";
        record.DraftGalleryMarkdown = parsed.Housing?.GalleryMarkdown ?? "";
        record.DraftLeaderboardCategory = parsed.Leaderboard?.Category.Trim() ?? "";
        record.DraftSeasonEntryId = parsed.Leaderboard?.SeasonEntryId;
    }

    private static CommunityRegistrationWriteResult ToRegistrationWrite(CommunityEventRegistration registration) =>
        new(registration.Id, registration.DisplayName, registration.Contact, registration.State, registration.RowVersion);

    private static ApplicationError? CheckPrecondition(uint current, byte[]? expected, string message = "Community record changed; reload before editing.") =>
        expected is null ? new ApplicationError("http.precondition-required", "If-Match is required.")
            : !ConcurrencyVersion.Matches(current, expected) ? new ApplicationError("http.precondition-failed", message) : null;

    private static bool TryParseKind(string? value, bool allowAll, out CommunityKind? kind)
    {
        if (allowAll && string.IsNullOrWhiteSpace(value)) { kind = null; return true; }
        if (Enum.TryParse<CommunityKind>(value, true, out var parsed) && Enum.IsDefined(parsed)) { kind = parsed; return true; }
        kind = null; return false;
    }

    private static void Merge(Dictionary<string, string[]> target, Dictionary<string, string[]> source)
    { foreach (var item in source) target[item.Key] = item.Value; }

    private static ApplicationError KindError() => new("community.kind.invalid", "Community kind is not supported.");
    private static ApplicationError ValidationError(Dictionary<string, string[]> errors) => new("validation.failed", "One or more validation errors occurred.", errors);
    private static ApplicationResult<T> KindFailure<T>() => ApplicationResult<T>.Failure(KindError());
    private static ApplicationResult<T> Invalid<T>(string message) => ApplicationResult<T>.Failure(new ApplicationError("validation.failed", message));
    private static ApplicationResult<T> ValidationFailure<T>(Dictionary<string, string[]> errors) => ApplicationResult<T>.Failure(ValidationError(errors));
    private static ApplicationResult<T> Conflict<T>(string message) => Failure<T>("community.record.conflict", message);
    private static ApplicationResult<T> Failure<T>(string code, string message) => ApplicationResult<T>.Failure(new ApplicationError(code, message));
    private static ApplicationResult<T> Success<T>(T value) => ApplicationResult<T>.Success(value);

    private sealed record ParsedCommunity(
        CommunityCommonInput Common, CommunityEventInput? Event, GuildInput? Guild,
        PlayerInput? Player, HousingInput? Housing, LeaderboardInput? Leaderboard);
}
