using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Features.Media;
using NovaHaven.Application.Features.Media.Repositories;
using NovaHaven.Application.Features.Media.Services;
using NovaHaven.Domain.Wiki.Entities;
using Xunit;

namespace NovaHaven.Domain.Tests;

public sealed class WikiMediaServiceTests
{
    private static readonly byte[] TinyPng = Convert.FromHexString(
        "89504E470D0A1A0A0000000D49484452000000010000000108060000001F15C489" +
        "0000000D49444154789C6360000000020001E221BC330000000049454E44AE426082");

    [Fact]
    public async Task Upload_rejects_spoofed_content_before_storage_or_persistence()
    {
        var repository = new StubMediaRepository();
        var storage = new StubMediaStorage();
        var unitOfWork = new StubUnitOfWork();
        var service = new WikiMediaService(repository, storage, unitOfWork);

        var result = await service.UploadAsync(TinyPng, "fake.jpg", "image/jpeg", Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("wiki.media.invalid", result.Error!.Code);
        Assert.Equal(0, storage.SaveCount);
        Assert.Equal(0, unitOfWork.SaveCount);
        Assert.Null(repository.AddedMedia);
    }

    [Fact]
    public async Task Upload_saves_verified_png_with_safe_metadata_and_audit()
    {
        var repository = new StubMediaRepository();
        var storage = new StubMediaStorage();
        var unitOfWork = new StubUnitOfWork();
        var actorId = Guid.NewGuid();
        var service = new WikiMediaService(repository, storage, unitOfWork);

        var result = await service.UploadAsync(TinyPng, "../ art.png ", "image/png", actorId, CancellationToken.None);

        Assert.True(result.IsSuccess);
        Assert.Equal("art.png", result.Value!.OriginalFileName);
        Assert.Equal("image/png", result.Value.ContentType);
        Assert.Equal(TinyPng.LongLength, result.Value.Length);
        Assert.Equal(1, result.Value.Width);
        Assert.Equal(1, result.Value.Height);
        Assert.Equal(1, storage.SaveCount);
        Assert.Equal(1, unitOfWork.SaveCount);
        Assert.Equal(actorId, repository.LastActorId);
        Assert.Equal("media.uploaded", repository.LastAuditAction);
    }

    [Fact]
    public async Task Upload_removes_local_blob_when_metadata_persistence_fails()
    {
        var repository = new StubMediaRepository();
        var storage = new StubMediaStorage();
        var unitOfWork = new StubUnitOfWork { SaveException = new InvalidOperationException("local db failure") };
        var service = new WikiMediaService(repository, storage, unitOfWork);

        await Assert.ThrowsAsync<InvalidOperationException>(() => service.UploadAsync(
            TinyPng, "art.png", "image/png", Guid.NewGuid(), CancellationToken.None));

        Assert.Equal(1, storage.SaveCount);
        Assert.Equal(1, storage.DeleteCount);
    }

    [Fact]
    public async Task Public_read_does_not_open_storage_for_private_media()
    {
        var storage = new StubMediaStorage();
        var service = new WikiMediaService(new StubMediaRepository
        {
            PublicMedia = null
        }, storage, new StubUnitOfWork());

        var result = await service.ReadPublishedAsync(Guid.NewGuid(), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal("wiki.media.not-found", result.Error!.Code);
        Assert.Equal(0, storage.OpenCount);
    }

    private sealed class StubUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Exception? SaveException { get; init; }

        public Task<int> SaveChangesAsync(CancellationToken cancellationToken)
        {
            SaveCount++;
            if (SaveException is not null) throw SaveException;
            return Task.FromResult(1);
        }

        public Task<T> ExecuteInTransactionAsync<T>(Func<CancellationToken, Task<T>> operation,
            Func<T, bool> shouldCommit, TransactionIsolation isolation, CancellationToken cancellationToken) =>
            throw new NotSupportedException();
    }

    private sealed class StubMediaStorage : IWikiMediaStorage
    {
        public int SaveCount { get; private set; }
        public int DeleteCount { get; private set; }
        public int OpenCount { get; private set; }

        public Task SaveAsync(string storageKey, ReadOnlyMemory<byte> bytes, CancellationToken cancellationToken)
        {
            SaveCount++;
            return Task.CompletedTask;
        }

        public Task<Stream?> OpenReadAsync(string storageKey, CancellationToken cancellationToken)
        {
            OpenCount++;
            return Task.FromResult<Stream?>(new MemoryStream(TinyPng));
        }

        public Task DeleteAsync(string storageKey, CancellationToken cancellationToken)
        {
            DeleteCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class StubMediaRepository : IWikiMediaRepository
    {
        public WikiMedia? AddedMedia { get; private set; }
        public WikiMedia? PublicMedia { get; init; } = new WikiMedia { StorageKey = "public.png", ContentType = "image/png" };
        public Guid? LastActorId { get; private set; }
        public string? LastAuditAction { get; private set; }

        public Task<IReadOnlyList<WikiMediaListItemResult>> ListAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<WikiMediaListItemResult>>([]);

        public Task<WikiMedia?> FindAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(AddedMedia?.Id == id ? AddedMedia : null);

        public Task<WikiMedia?> FindCurrentlyPublishedAsync(Guid id, CancellationToken cancellationToken) =>
            Task.FromResult(PublicMedia?.Id == id ? PublicMedia : null);

        public void Add(WikiMedia media) => AddedMedia = media;

        public void AddAudit(Guid? actorId, Guid mediaId, string contentType, long length, int width, int height)
        {
            LastActorId = actorId;
            LastAuditAction = "media.uploaded";
        }
    }
}
