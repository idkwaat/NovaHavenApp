namespace NovaHaven.Application.Common.Results;

public sealed class ApplicationResult<T>
{
    private ApplicationResult(bool isSuccess, T? value, ApplicationError? error)
    {
        IsSuccess = isSuccess;
        Value = value;
        Error = error;
    }

    public bool IsSuccess { get; }

    public T? Value { get; }

    public ApplicationError? Error { get; }

    public static ApplicationResult<T> Success(T value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new ApplicationResult<T>(true, value, null);
    }

    public static ApplicationResult<T> Failure(ApplicationError error)
    {
        ArgumentNullException.ThrowIfNull(error);
        return new ApplicationResult<T>(false, default, error);
    }
}
