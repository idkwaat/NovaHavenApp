using Microsoft.Data.SqlClient;

namespace NovaHaven.Api.Infrastructure;

/// <summary>Recognizes SQL Server's deadlock victim error even if EF wraps it.</summary>
public static class SqlServerConflict
{
    public static bool IsDeadlock(Exception error)
    {
        for (Exception? current = error; current is not null; current = current.InnerException)
        {
            if (current is SqlException { Number: 1205 }) return true;
        }

        return false;
    }
}
