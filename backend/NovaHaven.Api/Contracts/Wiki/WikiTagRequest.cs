namespace NovaHaven.Api.Contracts.Wiki;

public sealed record WikiTagRequest(string Name, string Slug);

public sealed record WikiTagUpdateRequest(string Name, string Slug, bool IsActive);

public sealed record AdminWikiTagResponse(Guid Id, string Name, string Slug, bool IsActive, string Etag);
