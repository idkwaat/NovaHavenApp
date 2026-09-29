namespace NovaHaven.Application.Common.Transactions;

public interface IUnitOfWork
{
    Task<int> SaveChangesAsync(CancellationToken cancellationToken);

    Task<T> ExecuteInTransactionAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        Func<T, bool> shouldCommit,
        TransactionIsolation isolation,
        CancellationToken cancellationToken);
}
