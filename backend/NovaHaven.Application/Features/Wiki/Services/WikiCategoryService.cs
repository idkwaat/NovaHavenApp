using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Features.Wiki.Repositories;
using NovaHaven.Application.Features.Wiki.Results;
using NovaHaven.Application.Wiki;
using NovaHaven.Domain.Wiki;
using NovaHaven.Domain.Wiki.Entities;

namespace NovaHaven.Application.Features.Wiki.Services;

public sealed class WikiCategoryService(IWikiCategoryRepository repository, IUnitOfWork unitOfWork)
{
    public async Task<ApplicationResult<IReadOnlyList<WikiCategoryAdminResult>>> ListAsync(
        CancellationToken cancellationToken)
    {
        var categories = await repository.ListAsync(cancellationToken);
        IReadOnlyList<WikiCategoryAdminResult> results = categories.Select(ToResult).ToArray();
        return ApplicationResult<IReadOnlyList<WikiCategoryAdminResult>>.Success(results);
    }

    public async Task<ApplicationResult<WikiCategoryAdminResult>> GetAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var category = await repository.FindAsync(id, cancellationToken);
        return category is null
            ? Failure<WikiCategoryAdminResult>("wiki.category.not-found", "The requested category was not found.")
            : ApplicationResult<WikiCategoryAdminResult>.Success(ToResult(category));
    }

    public async Task<ApplicationResult<WikiCategoryAdminResult>> CreateAsync(
        WikiCategoryInput input,
        CancellationToken cancellationToken)
    {
        var errors = WikiDraftValidator.ValidateCategory(input);
        if (errors.Count > 0) return ValidationFailure<WikiCategoryAdminResult>(errors);

        var normalizedName = WikiCategoryPolicy.NormalizeName(input.Name);
        if (await repository.HasDuplicateAsync(normalizedName, input.Slug, null, cancellationToken))
            return Failure<WikiCategoryAdminResult>("wiki.category.conflict", "Category name or slug already exists.");

        var category = new WikiCategory
        {
            Name = input.Name.Trim(),
            NormalizedName = normalizedName,
            Slug = input.Slug,
            DisplayOrder = input.DisplayOrder
        };
        repository.Add(category);
        await unitOfWork.SaveChangesAsync(cancellationToken);
        return ApplicationResult<WikiCategoryAdminResult>.Success(ToResult(category));
    }

    public Task<ApplicationResult<WikiCategoryAdminResult>> UpdateAsync(
        Guid id,
        WikiCategoryUpdateInput input,
        byte[]? expectedVersion,
        CancellationToken cancellationToken) => unitOfWork.ExecuteInTransactionAsync(
            async transactionToken =>
            {
                var category = await repository.FindForUpdateAsync(id, transactionToken);
                if (category is null)
                    return Failure<WikiCategoryAdminResult>("wiki.category.not-found", "The requested category was not found.");
                if (expectedVersion is null)
                    return Failure<WikiCategoryAdminResult>("http.precondition-required", "If-Match is required.");
                if (!category.RowVersion.AsSpan().SequenceEqual(expectedVersion))
                    return Failure<WikiCategoryAdminResult>("http.precondition-failed", "Category changed; reload before editing.");

                var errors = WikiDraftValidator.ValidateCategory(input);
                if (errors.Count > 0) return ValidationFailure<WikiCategoryAdminResult>(errors);

                var hasArticleReferences = await repository.HasArticleReferencesAsync(id, transactionToken);
                var hasRevisionReferences = await repository.HasRevisionReferencesAsync(id, transactionToken);
                var violation = WikiCategoryPolicy.CheckUpdate(category, input, hasArticleReferences, hasRevisionReferences);
                if (violation is not null)
                    return Failure<WikiCategoryAdminResult>("wiki.category.conflict", violation);

                var normalizedName = WikiCategoryPolicy.NormalizeName(input.Name);
                if (await repository.HasDuplicateAsync(normalizedName, input.Slug, id, transactionToken))
                    return Failure<WikiCategoryAdminResult>(
                        "wiki.category.conflict", "Category name or slug already exists.");

                category.Name = input.Name.Trim();
                category.NormalizedName = normalizedName;
                category.Slug = input.Slug;
                category.DisplayOrder = input.DisplayOrder;
                category.IsActive = input.IsActive;
                await unitOfWork.SaveChangesAsync(transactionToken);
                return ApplicationResult<WikiCategoryAdminResult>.Success(ToResult(category));
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
                var category = await repository.FindForUpdateAsync(id, transactionToken);
                if (category is null)
                    return Failure<bool>("wiki.category.not-found", "The requested category was not found.");
                if (expectedVersion is null)
                    return Failure<bool>("http.precondition-required", "If-Match is required.");
                if (!category.RowVersion.AsSpan().SequenceEqual(expectedVersion))
                    return Failure<bool>("http.precondition-failed", "Category changed; reload before editing.");

                var hasArticleReferences = await repository.HasArticleReferencesAsync(id, transactionToken);
                var hasRevisionReferences = await repository.HasRevisionReferencesAsync(id, transactionToken);
                var violation = WikiCategoryPolicy.CheckDelete(hasArticleReferences, hasRevisionReferences);
                if (violation is not null) return Failure<bool>("wiki.category.conflict", violation);

                repository.Remove(category);
                await unitOfWork.SaveChangesAsync(transactionToken);
                return ApplicationResult<bool>.Success(true);
            },
            result => result.IsSuccess,
            TransactionIsolation.Serializable,
            cancellationToken);

    private static WikiCategoryAdminResult ToResult(WikiCategory category) =>
        new(category.Id, category.Name, category.Slug, category.DisplayOrder, category.IsActive, category.RowVersion);

    private static ApplicationResult<T> ValidationFailure<T>(Dictionary<string, string[]> errors) =>
        Failure<T>("validation.failed", "One or more validation errors occurred.", errors);

    private static ApplicationResult<T> Failure<T>(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? validationErrors = null) =>
        ApplicationResult<T>.Failure(new ApplicationError(code, message, validationErrors));
}
