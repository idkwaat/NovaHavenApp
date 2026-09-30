using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Features.Audit.Queries;
using NovaHaven.Application.Features.Audit.Repositories;
using NovaHaven.Application.Features.Audit.Results;

namespace NovaHaven.Application.Features.Audit.Services;

public sealed class AuditService(IAuditEventRepository repository)
{
    public Task<ApplicationResult<AuditPageResult>> ListAsync(
        AuditListQuery query,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(query);

        if (query.Page < 1 || query.PageSize is < 1 or > 100)
        {
            return Task.FromResult(ApplicationResult<AuditPageResult>.Failure(
                new ApplicationError("validation.failed", "Invalid audit pagination.")));
        }

        var offset = ((long)query.Page - 1) * query.PageSize;
        if (offset > int.MaxValue)
        {
            return Task.FromResult(ApplicationResult<AuditPageResult>.Failure(
                new ApplicationError("validation.failed", "Page is out of range.")));
        }

        return ListCoreAsync(query, cancellationToken);
    }

    private async Task<ApplicationResult<AuditPageResult>> ListCoreAsync(
        AuditListQuery query,
        CancellationToken cancellationToken)
    {
        var result = await repository.ListAsync(query, cancellationToken);
        return ApplicationResult<AuditPageResult>.Success(result);
    }
}
