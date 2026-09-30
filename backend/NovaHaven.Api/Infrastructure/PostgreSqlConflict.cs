using Npgsql;

namespace NovaHaven.Api.Infrastructure;

public static class PostgreSqlConflict
{
    public static bool IsDeadlock(Exception error) =>
        HasSqlState(error, PostgresErrorCodes.DeadlockDetected);

    public static bool IsSerializationFailure(Exception error) =>
        HasSqlState(error, PostgresErrorCodes.SerializationFailure);

    public static bool IsUniqueViolation(Exception error) =>
        HasSqlState(error, PostgresErrorCodes.UniqueViolation);

    public static bool IsForeignKeyViolation(Exception error) =>
        HasSqlState(error, PostgresErrorCodes.ForeignKeyViolation);

    private static bool HasSqlState(Exception error, string expectedSqlState)
    {
        for (Exception? current = error; current is not null; current = current.InnerException)
        {
            if (current is PostgresException { SqlState: var sqlState }
                && string.Equals(sqlState, expectedSqlState, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
