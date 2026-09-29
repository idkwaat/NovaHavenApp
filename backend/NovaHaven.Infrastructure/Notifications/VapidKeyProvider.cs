using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using WebPush;

namespace NovaHaven.Infrastructure.Notifications;

public interface IVapidKeyProvider
{
    Task<VapidDetails?> GetAsync(CancellationToken cancellationToken = default);
}

public sealed class VapidKeyProvider(IConfiguration configuration, IHostEnvironment environment) : IVapidKeyProvider
{
    private static readonly SemaphoreSlim Gate = new(1, 1);
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<VapidDetails?> GetAsync(CancellationToken cancellationToken = default)
    {
        var subject = configuration["WebPush:Subject"] ?? "mailto:dev@novahaven.local";
        var publicKey = configuration["WebPush:PublicKey"];
        var privateKey = configuration["WebPush:PrivateKey"];
        if (!string.IsNullOrWhiteSpace(publicKey) && !string.IsNullOrWhiteSpace(privateKey))
            return new VapidDetails(subject, publicKey, privateKey);
        if (!environment.IsDevelopment() || !string.IsNullOrWhiteSpace(publicKey) || !string.IsNullOrWhiteSpace(privateKey)) return null;

        await Gate.WaitAsync(cancellationToken);
        try
        {
            var path = LocalKeyPath();
            if (File.Exists(path))
            {
                var saved = JsonSerializer.Deserialize<SavedVapidKeys>(await File.ReadAllTextAsync(path, cancellationToken), JsonOptions);
                if (saved is { PublicKey.Length: > 0, PrivateKey.Length: > 0, Subject.Length: > 0 })
                    return new VapidDetails(saved.Subject, saved.PublicKey, saved.PrivateKey);
            }

            var generated = VapidHelper.GenerateVapidKeys();
            var keys = new SavedVapidKeys(subject, generated.PublicKey, generated.PrivateKey);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            var temporaryPath = $"{path}.{Guid.NewGuid():N}.tmp";
            await File.WriteAllTextAsync(temporaryPath, JsonSerializer.Serialize(keys, JsonOptions), cancellationToken);
            File.Move(temporaryPath, path, overwrite: true);
            return new VapidDetails(keys.Subject, keys.PublicKey, keys.PrivateKey);
        }
        finally { Gate.Release(); }
    }

    private string LocalKeyPath()
    {
        if (configuration["WebPush:LocalKeyPath"] is { Length: > 0 } path) return Path.GetFullPath(path);
        var root = new DirectoryInfo(environment.ContentRootPath);
        while (root.Parent is not null && !File.Exists(Path.Combine(root.FullName, "AGENTS.md"))) root = root.Parent;
        return Path.Combine(root.FullName, ".local", "webpush-vapid.json");
    }

    private sealed record SavedVapidKeys(string Subject, string PublicKey, string PrivateKey);
}
