using NovaHaven.Application.Features.Operations.Repositories;
using NovaHaven.Application.Features.Operations.Results;

namespace NovaHaven.Application.Features.Operations.Services;

public sealed class OperationsService(IOperationsReadRepository repository)
{
    public async Task<OperationsDiagnosticsResult> GetDiagnosticsAsync(CancellationToken cancellationToken)
    {
        var data = await repository.GetDiagnosticsAsync(cancellationToken);
        return new OperationsDiagnosticsResult(DateTimeOffset.UtcNow, data.Database, data.Content, data.Integrations);
    }
}
