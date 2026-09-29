using Microsoft.Extensions.Configuration;
using NovaHaven.Application.Features.Media;

namespace NovaHaven.Infrastructure.Media;

public sealed class LocalWikiMediaStorage(IConfiguration configuration) : IWikiMediaStorage
{
    private readonly string rootPath = ResolveRoot(configuration);

    public async Task SaveAsync(string storageKey, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
    {
        var path = ResolvePath(storageKey);
        Directory.CreateDirectory(rootPath);
        await using var stream = new FileStream(path, new FileStreamOptions
        {
            Access = FileAccess.Write,
            Mode = FileMode.CreateNew,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            Share = FileShare.None
        });
        await stream.WriteAsync(bytes, cancellationToken);
    }

    public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storageKey);
        if (!File.Exists(path)) return Task.FromResult<Stream?>(null);
        Stream stream = new FileStream(path, new FileStreamOptions
        {
            Access = FileAccess.Read,
            Mode = FileMode.Open,
            Options = FileOptions.Asynchronous | FileOptions.SequentialScan,
            Share = FileShare.Read
        });
        return Task.FromResult<Stream?>(stream);
    }

    public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var path = ResolvePath(storageKey);
        if (File.Exists(path)) File.Delete(path);
        return Task.CompletedTask;
    }

    private string ResolvePath(string storageKey)
    {
        if (string.IsNullOrWhiteSpace(storageKey) || storageKey != Path.GetFileName(storageKey) ||
            storageKey.Contains("..", StringComparison.Ordinal) || storageKey.Contains(Path.DirectorySeparatorChar) ||
            storageKey.Contains(Path.AltDirectorySeparatorChar))
            throw new InvalidOperationException("Invalid media storage key.");
        return Path.Combine(rootPath, storageKey);
    }

    private static string ResolveRoot(IConfiguration configuration)
    {
        var configured = configuration["Media:RootPath"];
        var root = string.IsNullOrWhiteSpace(configured)
            ? Path.Combine(AppContext.BaseDirectory, "App_Data", "media")
            : (Path.IsPathRooted(configured) ? configured : Path.Combine(AppContext.BaseDirectory, configured));
        return Path.GetFullPath(root);
    }
}
