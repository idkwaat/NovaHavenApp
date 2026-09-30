using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Features.Commerce.Repositories;
using NovaHaven.Application.Features.Commerce.Results;
using NovaHaven.Domain.Commerce.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Infrastructure.Persistence.Repositories.Commerce;

public sealed class EfCommerceOrderRepository(NovaDbContext dbContext) : ICommerceOrderRepository
{
    public Task<CommerceOrder?> FindByIdempotencyKeyAsync(Guid key, CancellationToken cancellationToken) =>
        dbContext.CommerceOrders.AsNoTracking().SingleOrDefaultAsync(order => order.IdempotencyKey == key, cancellationToken);

    public async Task<IReadOnlyList<CommercePublishedOfferForCheckoutResult>> FindPublishedOffersAsync(
        IReadOnlyList<string> slugs, CancellationToken cancellationToken) =>
        await dbContext.CommerceOfferRevisions.AsNoTracking()
            .Where(revision => slugs.Contains(revision.Slug)
                && dbContext.CommerceOffers.Any(offer => offer.Id == revision.OfferId
                    && offer.State == CommerceOfferState.Published && offer.PublishedRevisionId == revision.Id))
            .Select(revision => new CommercePublishedOfferForCheckoutResult(
                revision.OfferId, revision.Slug, revision.Id, revision.Number, revision.Name,
                revision.IsPurchasable, revision.PriceMinorUnits))
            .ToArrayAsync(cancellationToken);

    public async Task<IReadOnlyList<CommerceOrderLineResult>> ListOrderLinesAsync(
        Guid orderId, CancellationToken cancellationToken) =>
        await dbContext.CommerceOrderLines.AsNoTracking().Where(line => line.OrderId == orderId)
            .OrderBy(line => line.OfferName)
            .Select(line => new CommerceOrderLineResult(line.OfferSlug, line.OfferName,
                line.RevisionNumber, line.Quantity, line.UnitPriceMinorUnits, line.LineTotalMinorUnits))
            .ToArrayAsync(cancellationToken);

    public Task<int> CountOrdersAsync(CancellationToken cancellationToken) =>
        dbContext.CommerceOrders.AsNoTracking().CountAsync(cancellationToken);

    public async Task<IReadOnlyList<CommerceOrderHistoryItemResult>> ListOrdersAsync(
        int offset, int pageSize, CancellationToken cancellationToken)
    {
        var orders = await dbContext.CommerceOrders.AsNoTracking()
            .OrderByDescending(order => order.CreatedAt).ThenByDescending(order => order.Id)
            .Skip(offset).Take(pageSize)
            .Select(order => new { order.Id, order.OrderNumber, order.CreatedAt, order.TotalMinorUnits, order.CurrencyCode })
            .ToArrayAsync(cancellationToken);
        var orderIds = orders.Select(order => order.Id).ToArray();
        var lines = await dbContext.CommerceOrderLines.AsNoTracking().Where(line => orderIds.Contains(line.OrderId))
            .OrderBy(line => line.OfferName)
            .Select(line => new
            {
                line.OrderId, line.OfferSlug, line.OfferName, line.RevisionNumber,
                line.Quantity, line.UnitPriceMinorUnits, line.LineTotalMinorUnits
            }).ToArrayAsync(cancellationToken);
        return orders.Select(order => new CommerceOrderHistoryItemResult(
            order.OrderNumber, order.CreatedAt, order.TotalMinorUnits, order.CurrencyCode,
            lines.Where(line => line.OrderId == order.Id).Select(line => new CommerceOrderLineResult(
                line.OfferSlug, line.OfferName, line.RevisionNumber, line.Quantity,
                line.UnitPriceMinorUnits, line.LineTotalMinorUnits)).ToArray())).ToArray();
    }

    public void Add(CommerceOrder order, IReadOnlyList<CommerceOrderLine> lines, CommercePayment payment)
    {
        dbContext.CommerceOrders.Add(order);
        dbContext.CommerceOrderLines.AddRange(lines);
        dbContext.CommercePayments.Add(payment);
    }

    public void ClearTrackedState() => dbContext.ChangeTracker.Clear();
}
