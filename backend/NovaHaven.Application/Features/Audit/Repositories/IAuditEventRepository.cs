using NovaHaven.Application.Features.Audit.Queries;
using NovaHaven.Application.Features.Audit.Results;

namespace NovaHaven.Application.Features.Audit.Repositories;

public interface IAuditEventRepository
{
    Task<AuditPageResult> ListAsync(AuditListQuery query, CancellationToken cancellationToken);
}
