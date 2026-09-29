using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Common.Results;
using NovaHaven.Domain.Wiki.Entities;
using NovaHaven.Infrastructure.Data;
using NovaHaven.Infrastructure.Persistence.UnitOfWork;
using Xunit;

namespace NovaHaven.Integration.Tests;

public sealed class UnitOfWorkIntegrationTests : IClassFixture<LocalApiFactory>
{
    private readonly LocalApiFactory factory;

    public UnitOfWorkIntegrationTests(LocalApiFactory factory) => this.factory = factory;

    [Fact]
    public async Task ExecuteInTransaction_rolls_back_saved_changes_when_operation_throws()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NovaDbContext>();
        var unitOfWork = new EfUnitOfWork(db);
        var slug = $"rollback-{Guid.NewGuid():N}";

        async Task<int> WriteThenFail(CancellationToken cancellationToken)
        {
            db.Categories.Add(new WikiCategory
            {
                Name = "Rollback probe",
                NormalizedName = slug.ToUpperInvariant(),
                Slug = slug
            });
            await unitOfWork.SaveChangesAsync(cancellationToken);
            throw new InvalidOperationException("Rollback probe failure.");
        }

        await Assert.ThrowsAsync<InvalidOperationException>(() => unitOfWork.ExecuteInTransactionAsync(
            WriteThenFail,
            _ => true,
            TransactionIsolation.ReadCommitted,
            CancellationToken.None));

        db.ChangeTracker.Clear();
        Assert.False(await db.Categories.AsNoTracking().AnyAsync(category => category.Slug == slug));
    }

    [Fact]
    public async Task ExecuteInTransaction_rolls_back_typed_failure_without_throwing()
    {
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<NovaDbContext>();
        var unitOfWork = new EfUnitOfWork(db);
        var slug = $"typed-rollback-{Guid.NewGuid():N}";
        var failure = ApplicationResult<int>.Failure(
            new ApplicationError("wiki.category.conflict", "The category already exists."));

        var result = await unitOfWork.ExecuteInTransactionAsync(
            async cancellationToken =>
            {
                db.Categories.Add(new WikiCategory
                {
                    Name = "Typed rollback probe",
                    NormalizedName = slug.ToUpperInvariant(),
                    Slug = slug
                });
                await unitOfWork.SaveChangesAsync(cancellationToken);
                return failure;
            },
            applicationResult => applicationResult.IsSuccess,
            TransactionIsolation.ReadCommitted,
            CancellationToken.None);

        Assert.Same(failure, result);
        db.ChangeTracker.Clear();
        Assert.False(await db.Categories.AsNoTracking().AnyAsync(category => category.Slug == slug));
    }
}
