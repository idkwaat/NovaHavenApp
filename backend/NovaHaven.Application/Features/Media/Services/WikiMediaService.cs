using System.Security.Cryptography;
using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Features.Media.Repositories;
using NovaHaven.Application.Features.Media.Results;
using NovaHaven.Domain.Wiki.Entities;

namespace NovaHaven.Application.Features.Media.Services;

public sealed class WikiMediaService(
    IWikiMediaRepository repository,
    IWikiMediaStorage storage,
    IUnitOfWork unitOfWork)
{
    public async Task<ApplicationResult<IReadOnlyList<WikiMediaListItemResult>>> ListAsync(
        CancellationToken cancellationToken)
    {
        var media = await repository.ListAsync(cancellationToken);
        return ApplicationResult<IReadOnlyList<WikiMediaListItemResult>>.Success(media);
    }

    public async Task<ApplicationResult<WikiMediaUploadResult>> UploadAsync(
        byte[] bytes,
        string originalFileName,
        string declaredContentType,
        Guid? actorId,
        CancellationToken cancellationToken)
    {
        if (bytes.Length > WikiMediaInspector.MaxBytes)
            return Invalid<WikiMediaUploadResult>("Image must not exceed 5 MB.");
        if (!WikiMediaInspector.TryInspect(bytes, declaredContentType, out var inspection))
            return Invalid<WikiMediaUploadResult>("Only valid PNG, JPEG or WebP images up to 5 MB are accepted.");

        var media = new WikiMedia
        {
            StorageKey = $"{Guid.NewGuid():N}{inspection.Extension}",
            OriginalFileName = NormalizeFileName(originalFileName),
            ContentType = inspection.ContentType,
            Length = bytes.LongLength,
            Sha256 = Convert.ToHexString(SHA256.HashData(bytes)),
            Width = inspection.Width,
            Height = inspection.Height,
            CreatedAt = DateTimeOffset.UtcNow
        };

        await storage.SaveAsync(media.StorageKey, bytes, cancellationToken);
        try
        {
            repository.Add(media);
            repository.AddAudit(actorId, media.Id, media.ContentType, media.Length, media.Width, media.Height);
            await unitOfWork.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            try
            {
                await storage.DeleteAsync(media.StorageKey, CancellationToken.None);
            }
            catch
            {
                // Keep the persistence failure visible; orphan cleanup can be retried by local maintenance.
            }

            throw;
        }

        return ApplicationResult<WikiMediaUploadResult>.Success(new WikiMediaUploadResult(
            media.Id, media.OriginalFileName, media.ContentType, media.Length, media.Width, media.Height));
    }

    public async Task<ApplicationResult<WikiMediaContentResult>> PreviewAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var media = await repository.FindAsync(id, cancellationToken);
        if (media is null) return NotFound<WikiMediaContentResult>();
        var stream = await storage.OpenReadAsync(media.StorageKey, cancellationToken);
        return stream is null
            ? NotFound<WikiMediaContentResult>()
            : ApplicationResult<WikiMediaContentResult>.Success(new WikiMediaContentResult(media.ContentType, stream));
    }

    public async Task<ApplicationResult<WikiMediaContentResult>> ReadPublishedAsync(
        Guid id,
        CancellationToken cancellationToken)
    {
        var media = await repository.FindCurrentlyPublishedAsync(id, cancellationToken);
        if (media is null) return NotFound<WikiMediaContentResult>();
        var stream = await storage.OpenReadAsync(media.StorageKey, cancellationToken);
        return stream is null
            ? NotFound<WikiMediaContentResult>()
            : ApplicationResult<WikiMediaContentResult>.Success(new WikiMediaContentResult(media.ContentType, stream));
    }

    private static string NormalizeFileName(string fileName)
    {
        var name = Path.GetFileName(fileName).Trim();
        return name.Length == 0 ? "upload" : name[..Math.Min(name.Length, 255)];
    }

    private static ApplicationResult<T> Invalid<T>(string message) =>
        ApplicationResult<T>.Failure(new ApplicationError("wiki.media.invalid", message));

    private static ApplicationResult<T> NotFound<T>() =>
        ApplicationResult<T>.Failure(new ApplicationError("wiki.media.not-found", "The requested media was not found."));
}
