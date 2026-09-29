namespace NovaHaven.Application.Common.Concurrency;

public interface IPersistenceConflictDetector
{
    bool IsRetryableConflict(Exception exception);
}
