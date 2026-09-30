using System.Security.Cryptography;
using System.Text;
using NovaHaven.Application.Common.Concurrency;
using NovaHaven.Application.Common.Results;
using NovaHaven.Application.Common.Transactions;
using NovaHaven.Application.Commerce;
using NovaHaven.Application.Features.Commerce.Commands;
using NovaHaven.Application.Features.Commerce.Repositories;
using NovaHaven.Application.Features.Commerce.Results;
using NovaHaven.Domain.Commerce.Entities;

namespace NovaHaven.Application.Features.Commerce.Services;

public sealed class CommerceOrderService(
    ICommerceOrderRepository repository,
    IUnitOfWork unitOfWork,
    IPersistenceConflictDetector conflictDetector)
{
    private const long MaximumPriceMinorUnits = 1_000_000_000_000;

    public async Task<ApplicationResult<CommerceOrderReceiptResult>> CheckoutAsync(
        string? idempotencyKeyValue, CommerceCheckoutCommand command, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(idempotencyKeyValue, out var idempotencyKey))
            return ValidationFailure<CommerceOrderReceiptResult>(new Dictionary<string, string[]>
                { ["Idempotency-Key"] = ["A UUID Idempotency-Key header is required."] });
        var input = new CommerceCheckoutInput(command.Items);
        var errors = CommerceCheckoutValidator.Validate(input);
        if (errors.Count > 0) return ValidationFailure<CommerceOrderReceiptResult>(errors);
        var items = input.Items!;
        var fingerprint = Fingerprint(items);
        var previous = await repository.FindByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
        if (previous is not null) return await ExistingReceiptAsync(previous, fingerprint, cancellationToken);

        try
        {
            return await unitOfWork.ExecuteInTransactionAsync(
                async token =>
                {
                    var prior = await repository.FindByIdempotencyKeyAsync(idempotencyKey, token);
                    if (prior is not null)
                        return await ExistingReceiptAsync(prior, fingerprint, token);

                    var offers = await repository.FindPublishedOffersAsync(items.Select(item => item.Slug).ToArray(), token);
                    if (offers.Count != items.Count)
                        return CheckoutConflict<CommerceOrderReceiptResult>("One or more offers are no longer published.");
                    var bySlug = offers.ToDictionary(offer => offer.Slug, StringComparer.Ordinal);
                    if (items.Any(item => !bySlug.TryGetValue(item.Slug, out var offer)
                        || !offer.IsPurchasable || offer.PriceMinorUnits is null or <= 0 or > MaximumPriceMinorUnits))
                        return CheckoutConflict<CommerceOrderReceiptResult>("One or more offers are not available for local demo checkout.");

                    var now = DateTimeOffset.UtcNow;
                    var order = new CommerceOrder
                    {
                        OrderNumber = CreateOrderNumber(now), IdempotencyKey = idempotencyKey,
                        RequestFingerprint = fingerprint, CreatedAt = now
                    };
                    var lines = new List<CommerceOrderLine>(items.Count);
                    long total = 0;
                    foreach (var item in items.OrderBy(line => line.Slug, StringComparer.Ordinal))
                    {
                        var offer = bySlug[item.Slug];
                        var unitPrice = offer.PriceMinorUnits!.Value;
                        var lineTotal = checked(unitPrice * item.Quantity);
                        total = checked(total + lineTotal);
                        lines.Add(new CommerceOrderLine
                        {
                            OrderId = order.Id, OfferId = offer.OfferId, OfferRevisionId = offer.RevisionId,
                            RevisionNumber = offer.Revision, OfferSlug = offer.Slug, OfferName = offer.Name,
                            Quantity = item.Quantity, UnitPriceMinorUnits = unitPrice, LineTotalMinorUnits = lineTotal
                        });
                    }
                    order.TotalMinorUnits = total;
                    var payment = new CommercePayment
                    {
                        OrderId = order.Id, Method = CommercePaymentMethod.LocalDemo,
                        Status = CommercePaymentStatus.Simulated, AmountMinorUnits = total,
                        CurrencyCode = order.CurrencyCode, CreatedAt = now
                    };
                    repository.Add(order, lines, payment);
                    await unitOfWork.SaveChangesAsync(token);
                    return Success(new CommerceOrderReceiptResult(order.OrderNumber, order.CreatedAt,
                        order.TotalMinorUnits, order.CurrencyCode, lines.OrderBy(line => line.OfferName)
                        .Select(ToResult).ToArray(), false));
                }, result => result.IsSuccess, TransactionIsolation.Serializable, cancellationToken);
        }
        catch (Exception exception) when (conflictDetector.IsRetryableConflict(exception))
        {
            repository.ClearTrackedState();
            previous = await repository.FindByIdempotencyKeyAsync(idempotencyKey, cancellationToken);
            return previous is null
                ? CheckoutConflict<CommerceOrderReceiptResult>("Checkout conflicted with another local request; retry with the same key.")
                : await ExistingReceiptAsync(previous, fingerprint, cancellationToken);
        }
    }

    public async Task<ApplicationResult<CommerceOrderHistoryPageResult>> ListAdminAsync(
        int? requestedPage, int? requestedPageSize, CancellationToken cancellationToken)
    {
        var page = requestedPage ?? 1;
        var pageSize = requestedPageSize ?? 20;
        if (page < 1 || pageSize is < 1 or > 50) return Invalid<CommerceOrderHistoryPageResult>("Invalid order pagination.");
        var offset = ((long)page - 1) * pageSize;
        if (offset > int.MaxValue) return Invalid<CommerceOrderHistoryPageResult>("Page is out of range.");
        var total = await repository.CountOrdersAsync(cancellationToken);
        var items = await repository.ListOrdersAsync((int)offset, pageSize, cancellationToken);
        return Success(new CommerceOrderHistoryPageResult(items, page, pageSize, total));
    }

    private async Task<ApplicationResult<CommerceOrderReceiptResult>> ExistingReceiptAsync(
        CommerceOrder order, string fingerprint, CancellationToken cancellationToken)
    {
        if (!CryptographicOperations.FixedTimeEquals(
            Convert.FromHexString(order.RequestFingerprint), Convert.FromHexString(fingerprint)))
            return CheckoutConflict<CommerceOrderReceiptResult>("Idempotency-Key was already used for a different cart.");
        var lines = await repository.ListOrderLinesAsync(order.Id, cancellationToken);
        return Success(new CommerceOrderReceiptResult(order.OrderNumber, order.CreatedAt,
            order.TotalMinorUnits, order.CurrencyCode, lines, true));
    }

    private static string Fingerprint(IReadOnlyList<CommerceCheckoutLineInput> items)
    {
        var normalized = string.Join('\n', items.OrderBy(item => item.Slug, StringComparer.Ordinal)
            .Select(item => $"{item.Slug}\0{item.Quantity}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private static string CreateOrderNumber(DateTimeOffset createdAt) =>
        $"NH-D-{createdAt:yyyyMMdd}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(6))}";

    private static CommerceOrderLineResult ToResult(CommerceOrderLine line) => new(
        line.OfferSlug, line.OfferName, line.RevisionNumber, line.Quantity,
        line.UnitPriceMinorUnits, line.LineTotalMinorUnits);

    private static ApplicationResult<T> CheckoutConflict<T>(string message) =>
        ApplicationResult<T>.Failure(new ApplicationError("commerce.checkout.conflict", message));
    private static ApplicationResult<T> Invalid<T>(string message) =>
        ApplicationResult<T>.Failure(new ApplicationError("validation.failed", message));
    private static ApplicationResult<T> ValidationFailure<T>(Dictionary<string, string[]> errors) =>
        ApplicationResult<T>.Failure(new ApplicationError("validation.failed", "One or more validation errors occurred.", errors));
    private static ApplicationResult<T> Success<T>(T value) => ApplicationResult<T>.Success(value);
}
