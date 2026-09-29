using System.Data;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Infrastructure.Persistence.UnitOfWork;

public sealed class EfUnitOfWork(NovaDbContext dbContext) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken) =>
        dbContext.SaveChangesAsync(cancellationToken);

    public async Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<T, bool> shouldCommit,
        TransactionIsolation isolation,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(shouldCommit);

        await using var transaction = await dbContext.Database.BeginTransactionAsync(
            ToIsolationLevel(isolation), cancellationToken);

        try
        {
            var result = await operation(cancellationToken);
            if (shouldCommit(result))
            {
                await transaction.CommitAsync(cancellationToken);
            }
            else
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }

            return result;
        }
        catch
        {
            // Preserve the original callback/commit exception if rollback itself also fails.
            try
            {
                await transaction.RollbackAsync(CancellationToken.None);
            }
            catch
            {
                // Disposing the transaction remains a final rollback safeguard.
            }

            throw;
        }
    }

    private static IsolationLevel ToIsolationLevel(TransactionIsolation isolation) => isolation switch
    {
        TransactionIsolation.ReadUncommitted => IsolationLevel.ReadUncommitted,
        TransactionIsolation.ReadCommitted => IsolationLevel.ReadCommitted,
        TransactionIsolation.RepeatableRead => IsolationLevel.RepeatableRead,
        TransactionIsolation.Serializable => IsolationLevel.Serializable,
        _ => throw new ArgumentOutOfRangeException(nameof(isolation), isolation, "Unsupported transaction isolation level.")
    };
}
