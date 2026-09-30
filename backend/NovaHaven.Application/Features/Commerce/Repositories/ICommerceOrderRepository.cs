using NovaHaven.Application.Features.Commerce.Results;
using NovaHaven.Domain.Commerce.Entities;

namespace NovaHaven.Application.Features.Commerce.Repositories;

public interface ICommerceOrderRepository
{
    Task<CommerceOrder?> FindByIdempotencyKeyAsync(Guid key, CancellationToken cancellationToken);
    Task<IReadOnlyList<CommercePublishedOfferForCheckoutResult>> FindPublishedOffersAsync(
        IReadOnlyList<string> slugs, CancellationToken cancellationToken);
    Task<IReadOnlyList<CommerceOrderLineResult>> ListOrderLinesAsync(Guid orderId, CancellationToken cancellationToken);
    Task<int> CountOrdersAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<CommerceOrderHistoryItemResult>> ListOrdersAsync(
        int offset, int pageSize, CancellationToken cancellationToken);
    void Add(CommerceOrder order, IReadOnlyList<CommerceOrderLine> lines, CommercePayment payment);
    void ClearTrackedState();
}
