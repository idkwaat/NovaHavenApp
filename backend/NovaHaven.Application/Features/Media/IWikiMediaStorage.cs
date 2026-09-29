namespace NovaHaven.Application.Features.Media;

public interface IWikiMediaStorage
{
    Task SaveAsync(string storageKey, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken);

    Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken);

    Task DeleteAsync(string storageKey, CancellationToken cancellationToken);
}

public sealed record WikiMediaInspection(string ContentType, string Extension, int Width, int Height);
