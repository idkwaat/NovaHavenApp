using NovaHaven.Application.Features.Operations.Results;

namespace NovaHaven.Application.Features.Operations.Repositories;

public interface IOperationsReadRepository
{
    Task<OperationsDataResult> GetDiagnosticsAsync(CancellationToken cancellationToken);
}
