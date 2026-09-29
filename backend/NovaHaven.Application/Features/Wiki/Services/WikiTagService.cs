using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Features.Wiki.Repositories;
using NovaHaven.Application.Features.Wiki.Results;
using NovaHaven.Application.Wiki;
using NovaHaven.Domain.Wiki;
using NovaHaven.Domain.Wiki.Entities;

namespace NovaHaven.Application.Features.Wiki.Services;

public sealed class WikiTagService(IWikiTagRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<ApplicationResult<IReadOnlyList<WikiTagAdminResult>>> ListAsync(
        CancellationToken cancellationToken)
    {
        var tags = await repository.ListAsync(cancellationToken);
        IReadOnlyList<WikiTagAdminResult> results = tags.Select(ToResult).ToArray();
        return ApplicationResult<IReadOnlyList<WikiTagAdminResult>>.Success(results);
    }

    public async Task<ApplicationResult<WikiTagAdminResult>> CreateAsync(
        WikiTagInput input,
        CancellationToken cancellationToken)
    {
        var errors = WikiDraftValidator.ValidateTag(input);
        if (errors.Count > 0) return ValidationFailure(errors);

        var normalizedName = WikiTagPolicy.NormalizeName(input.Name);
        if (await repository.HasDuplicateAsync(normalizedName, input.Slug, null, cancellationToken))
            return Conflict("Tag name or slug already exists.");

        var tag = new WikiTag
        {
            Name = input.Name.Trim(),
            NormalizedName = normalizedName,
            Slug = input.Slug
        };
        repository.Add(tag);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<WikiTagAdminResult>.Success(ToResult(tag));
    }

    public Task<ApplicationResult<WikiTagAdminResult>> UpdateAsync(
        Guid id,
        WikiTagUpdateInput input,
        byte[]? expectedVersion,
        CancellationToken cancellationToken) => unitOfWork.ExecuteInTransactionAsync(
            async transactionToken =>
            {
                var tag = await repository.FindForUpdateAsync(id, transactionToken);
                if (tag is null) return NotFound();
                if (expectedVersion is null) return PreconditionRequired();
                if (!tag.RowVersion.AsSpan().SequenceEqual(expectedVersion)) return Stale();

                var errors = WikiDraftValidator.ValidateTag(input);
                if (errors.Count > 0) return ValidationFailure(errors);

                var hasDraftReferences = await repository.HasDraftReferencesAsync(id, transactionToken);
                var hasRevisionReferences = await repository.HasRevisionReferencesAsync(id, transactionToken);
                var violation = WikiTagPolicy.CheckUpdate(tag, input, hasDraftReferences, hasRevisionReferences);
                if (violation is not null) return Conflict(violation);

                var normalizedName = WikiTagPolicy.NormalizeName(input.Name);
                if (await repository.HasDuplicateAsync(normalizedName, input.Slug, id, transactionToken))
                    return Conflict("Tag name or slug already exists.");

                tag.Name = input.Name.Trim();
                tag.NormalizedName = normalizedName;
                tag.Slug = input.Slug;
                tag.IsActive = input.IsActive;
                await unitOfWork.SaveChangesAsync(transactionToken);
                return ApplicationResult<WikiTagAdminResult>.Success(ToResult(tag));
            },
            result => result.IsSuccess,
            TransactionIsolation.Serializable,
            cancellationToken);

    public Task<ApplicationResult<bool>> DeleteAsync(
        Guid id,
        byte[]? expectedVersion,
        CancellationToken cancellationToken) => unitOfWork.ExecuteInTransactionAsync(
            async transactionToken =>
            {
                var tag = await repository.FindForUpdateAsync(id, transactionToken);
                if (tag is null) return NotFoundBoolean();
                if (expectedVersion is null) return PreconditionRequiredBoolean();
                if (!tag.RowVersion.AsSpan().SequenceEqual(expectedVersion)) return StaleBoolean();

                var hasDraftReferences = await repository.HasDraftReferencesAsync(id, transactionToken);
                var hasRevisionReferences = await repository.HasRevisionReferencesAsync(id, transactionToken);
                var violation = WikiTagPolicy.CheckDelete(hasDraftReferences, hasRevisionReferences);
                if (violation is not null) return ConflictBoolean(violation);

                repository.Remove(tag);
                await unitOfWork.SaveChangesAsync(transactionToken);
                return ApplicationResult<bool>.Success(true);
            },
            result => result.IsSuccess,
            TransactionIsolation.Serializable,
            cancellationToken);

    private static WikiTagAdminResult ToResult(WikiTag tag) =>
        new(tag.Id, tag.Name, tag.Slug, tag.IsActive, tag.RowVersion);

    private static ApplicationResult<WikiTagAdminResult> ValidationFailure(Dictionary<string, string[]> errors) =>
        ApplicationResult<WikiTagAdminResult>.Failure(new ApplicationError(
            "validation.failed", "One or more validation errors occurred.", errors));

    private static ApplicationResult<WikiTagAdminResult> NotFound() =>
        ApplicationResult<WikiTagAdminResult>.Failure(
            new ApplicationError("wiki.tag.not-found", "The requested tag was not found."));

    private static ApplicationResult<WikiTagAdminResult> PreconditionRequired() =>
        ApplicationResult<WikiTagAdminResult>.Failure(
            new ApplicationError("http.precondition-required", "If-Match is required."));

    private static ApplicationResult<WikiTagAdminResult> Stale() =>
        ApplicationResult<WikiTagAdminResult>.Failure(
            new ApplicationError("http.precondition-failed", "Tag changed; reload before editing."));

    private static ApplicationResult<WikiTagAdminResult> Conflict(string message) =>
        ApplicationResult<WikiTagAdminResult>.Failure(new ApplicationError("wiki.tag.conflict", message));

    private static ApplicationResult<bool> NotFoundBoolean() =>
        ApplicationResult<bool>.Failure(new ApplicationError("wiki.tag.not-found", "The requested tag was not found."));

    private static ApplicationResult<bool> PreconditionRequiredBoolean() =>
        ApplicationResult<bool>.Failure(new ApplicationError("http.precondition-required", "If-Match is required."));

    private static ApplicationResult<bool> StaleBoolean() =>
        ApplicationResult<bool>.Failure(new ApplicationError("http.precondition-failed", "Tag changed; reload before editing."));

    private static ApplicationResult<bool> ConflictBoolean(string message) =>
        ApplicationResult<bool>.Failure(new ApplicationError("wiki.tag.conflict", message));
}
