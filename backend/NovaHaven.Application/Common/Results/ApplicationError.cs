using System.Collections.ObjectModel;

namespace NovaHaven.Application.Common.Results;

public sealed record ApplicationError
{
    public ApplicationError(
        string code,
        string message,
        IReadOnlyDictionary<string, string[]>? validationErrors = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Code = code;
        Message = message;
        ValidationErrors = validationErrors is null
            ? null
            : new ReadOnlyDictionary<string, string[]>(
                validationErrors.ToDictionary(
                    entry => entry.Key,
                    entry => entry.Value.ToArray(),
                    StringComparer.Ordinal));
    }

    public string Code { get; }

    public string Message { get; }

    public IReadOnlyDictionary<string, string[]>? ValidationErrors { get; }
}
