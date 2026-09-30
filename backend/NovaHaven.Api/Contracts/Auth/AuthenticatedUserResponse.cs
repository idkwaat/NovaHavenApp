namespace NovaHaven.Api.Contracts.Auth;

public sealed record AuthenticatedUserResponse(string Id, string? Email, bool IsAdmin);
