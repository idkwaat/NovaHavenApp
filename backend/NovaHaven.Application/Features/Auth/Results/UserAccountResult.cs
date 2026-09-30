namespace NovaHaven.Application.Features.Auth.Results;

public sealed record UserAccountResult(Guid Id, string? Email, bool IsAdmin);
