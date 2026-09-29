using Npgsql;
using NovaHaven.Application.Common.Concurrency;

namespace NovaHaven.Infrastructure.Persistence;

public sealed class PostgreSqlPersistenceConflictDetector : IPersistenceConflictDetector
{
    public bool IsRetryableConflict(Exception exception) =>
        HasSqlState(exception, PostgresErrorCodes.UniqueViolation)
        || HasSqlState(exception, PostgresErrorCodes.DeadlockDetected)
        || HasSqlState(exception, PostgresErrorCodes.SerializationFailure);

    private static bool HasSqlState(Exception exception, string expected)
    {
        for (Exception? current = exception; current is not null; current = current.InnerException)
            if (current is PostgresException { SqlState: var state } && state == expected)
                return true;
        return false;
    }
}
