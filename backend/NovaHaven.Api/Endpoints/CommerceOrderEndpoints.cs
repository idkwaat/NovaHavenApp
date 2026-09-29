using System.Data;
using System.Security.Cryptography;
using System.Text;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NovaHaven.Application.Commerce;
using NovaHaven.Domain.Commerce;
using NovaHaven.Domain.Commerce.Entities;
using NovaHaven.Infrastructure.Data;

namespace NovaHaven.Api.Endpoints;

public static class CommerceOrderEndpoints
{
    private const long MaximumPriceMinorUnits = 1_000_000_000_000;

    public static void MapCommerceOrderEndpoints(this WebApplication app)
    {
        app.MapPost("/api/v1/commerce/orders", CheckoutAsync);
        app.MapGet("/api/v1/admin/commerce/orders", ListAdminAsync).RequireAuthorization("AdminOnly");
    }

    private static async Task<IResult> CheckoutAsync(
        HttpContext http, NovaDbContext db, CommerceCheckoutRequest request, CancellationToken ct)
    {
        if (!http.Request.Headers.TryGetValue("Idempotency-Key", out var keyValue) ||
            !Guid.TryParse(keyValue.ToString(), out var idempotencyKey))
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["Idempotency-Key"] = ["A UUID Idempotency-Key header is required."]
            });

        var input = new CommerceCheckoutInput(request.Items);
        var errors = CommerceCheckoutValidator.Validate(input);
        if (errors.Count > 0) return Results.ValidationProblem(errors);
        var items = input.Items!;
        var fingerprint = Fingerprint(items);

        var previous = await db.CommerceOrders.AsNoTracking()
            .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, ct);
        if (previous is not null) return await ExistingReceiptAsync(db, previous, fingerprint, ct);

        await using var transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable, ct);
        try
        {
            previous = await db.CommerceOrders.AsNoTracking()
                .SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, ct);
            if (previous is not null)
            {
                await transaction.CommitAsync(ct);
                return await ExistingReceiptAsync(db, previous, fingerprint, ct);
            }

            var slugs = items.Select(x => x.Slug).ToArray();
            var offers = await (from offer in db.CommerceOffers.AsNoTracking()
                                join revision in db.CommerceOfferRevisions.AsNoTracking() on offer.PublishedRevisionId equals revision.Id
                                where offer.State == CommerceOfferState.Published && slugs.Contains(revision.Slug)
                                select new
                                {
                                    OfferId = offer.Id, PublishedSlug = revision.Slug, RevisionId = revision.Id, revision.Number,
                                    revision.Name, revision.IsPurchasable, revision.PriceMinorUnits
                                })
                .ToListAsync(ct);
            if (offers.Count != items.Count)
            {
                await transaction.RollbackAsync(ct);
                return Results.Problem(statusCode: 409, title: "One or more offers are no longer published.");
            }

            var offersBySlug = offers.ToDictionary(x => x.PublishedSlug, StringComparer.Ordinal);
            if (items.Any(item => !offersBySlug.TryGetValue(item.Slug, out var offer) ||
                                  !offer.IsPurchasable || offer.PriceMinorUnits is null or <= 0 or > MaximumPriceMinorUnits))
            {
                await transaction.RollbackAsync(ct);
                return Results.Problem(statusCode: 409, title: "One or more offers are not available for local demo checkout.");
            }

            long total = 0;
            var order = new CommerceOrder
            {
                OrderNumber = CreateOrderNumber(),
                IdempotencyKey = idempotencyKey,
                RequestFingerprint = fingerprint,
                CreatedAt = DateTimeOffset.UtcNow
            };
            var lines = new List<CommerceOrderLine>(items.Count);
            foreach (var item in items.OrderBy(x => x.Slug, StringComparer.Ordinal))
            {
                var offer = offersBySlug[item.Slug];
                var unitPrice = offer.PriceMinorUnits!.Value;
                var lineTotal = checked(unitPrice * item.Quantity);
                total = checked(total + lineTotal);
                lines.Add(new CommerceOrderLine
                {
                    OrderId = order.Id,
                    OfferId = offer.OfferId,
                    OfferRevisionId = offer.RevisionId,
                    RevisionNumber = offer.Number,
                    OfferSlug = offer.PublishedSlug,
                    OfferName = offer.Name,
                    Quantity = item.Quantity,
                    UnitPriceMinorUnits = unitPrice,
                    LineTotalMinorUnits = lineTotal
                });
            }
            order.TotalMinorUnits = total;
            db.CommerceOrders.Add(order);
            db.CommerceOrderLines.AddRange(lines);
            db.CommercePayments.Add(new CommercePayment
            {
                OrderId = order.Id,
                Method = CommercePaymentMethod.LocalDemo,
                Status = CommercePaymentStatus.Simulated,
                AmountMinorUnits = total,
                CurrencyCode = order.CurrencyCode,
                CreatedAt = order.CreatedAt
            });
            await db.SaveChangesAsync(ct);
            await transaction.CommitAsync(ct);
            return await ReceiptAsync(db, order, replay: false, StatusCodes.Status201Created, ct);
        }
        catch (DbUpdateException error) when (UniqueViolationOrDeadlock(error))
        {
            await RollbackQuietlyAsync(transaction, ct);
            db.ChangeTracker.Clear();
            previous = await db.CommerceOrders.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, ct);
            return previous is null
                ? Results.Problem(statusCode: 409, title: "Checkout conflicted with another local request; retry with the same key.")
                : await ExistingReceiptAsync(db, previous, fingerprint, ct);
        }
        catch (SqlException error) when (error.Number == 1205)
        {
            await RollbackQuietlyAsync(transaction, ct);
            db.ChangeTracker.Clear();
            previous = await db.CommerceOrders.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == idempotencyKey, ct);
            return previous is null
                ? Results.Problem(statusCode: 409, title: "Checkout conflicted with another local request; retry with the same key.")
                : await ExistingReceiptAsync(db, previous, fingerprint, ct);
        }
    }

    private static async Task<IResult> ExistingReceiptAsync(
        NovaDbContext db, CommerceOrder order, string fingerprint, CancellationToken ct)
    {
        if (!CryptographicOperations.FixedTimeEquals(
                Convert.FromHexString(order.RequestFingerprint), Convert.FromHexString(fingerprint)))
            return Results.Problem(statusCode: 409, title: "Idempotency-Key was already used for a different cart.");
        return await ReceiptAsync(db, order, replay: true, StatusCodes.Status200OK, ct);
    }

    private static async Task<IResult> ReceiptAsync(
        NovaDbContext db, CommerceOrder order, bool replay, int statusCode, CancellationToken ct)
    {
        var items = await db.CommerceOrderLines.AsNoTracking().Where(x => x.OrderId == order.Id)
            .OrderBy(x => x.OfferName)
            .Select(x => new
            {
                slug = x.OfferSlug, name = x.OfferName, revision = x.RevisionNumber, quantity = x.Quantity,
                unitPriceMinorUnits = x.UnitPriceMinorUnits, lineTotalMinorUnits = x.LineTotalMinorUnits
            }).ToListAsync(ct);
        var receipt = new
        {
            orderNumber = order.OrderNumber, createdAt = order.CreatedAt, status = "demoCompleted",
            totalMinorUnits = order.TotalMinorUnits, currencyCode = order.CurrencyCode, items,
            paymentStatus = "simulated", paymentMethod = "localDemo", realCharge = false,
            fulfilment = "none", idempotentReplay = replay,
            message = "Mô phỏng local — không thu tiền thật và không giao vật phẩm hay quyền lợi trong game."
        };
        return Results.Json(receipt, statusCode: statusCode);
    }

    private static async Task<IResult> ListAdminAsync(
        NovaDbContext db, int? page, int? pageSize, CancellationToken ct)
    {
        var currentPage = page ?? 1;
        var size = pageSize ?? 20;
        if (currentPage < 1 || size is < 1 or > 50)
            return Results.Problem(statusCode: 400, title: "Invalid order pagination.");
        var query = db.CommerceOrders.AsNoTracking();
        var total = await query.CountAsync(ct);
        var offset = ((long)currentPage - 1) * size;
        if (offset > int.MaxValue) return Results.Problem(statusCode: 400, title: "Page is out of range.");
        var orders = await query.OrderByDescending(x => x.CreatedAt).ThenByDescending(x => x.Id).Skip((int)offset).Take(size)
            .Select(x => new { x.Id, x.OrderNumber, x.CreatedAt, x.TotalMinorUnits, x.CurrencyCode, x.Status })
            .ToListAsync(ct);
        var orderIds = orders.Select(x => x.Id).ToArray();
        var lines = await db.CommerceOrderLines.AsNoTracking().Where(x => orderIds.Contains(x.OrderId))
            .Select(x => new
            {
                x.OrderId, slug = x.OfferSlug, name = x.OfferName, revision = x.RevisionNumber,
                quantity = x.Quantity, unitPriceMinorUnits = x.UnitPriceMinorUnits,
                lineTotalMinorUnits = x.LineTotalMinorUnits
            }).ToListAsync(ct);
        var result = orders.Select(order => new
        {
            orderNumber = order.OrderNumber, createdAt = order.CreatedAt, status = "demoCompleted",
            totalMinorUnits = order.TotalMinorUnits, currencyCode = order.CurrencyCode,
            paymentStatus = "simulated", paymentMethod = "localDemo", realCharge = false, fulfilment = "none",
            items = lines.Where(line => line.OrderId == order.Id).Select(line => new
            {
                line.slug, line.name, line.revision, line.quantity, line.unitPriceMinorUnits, line.lineTotalMinorUnits
            }).ToArray()
        }).ToArray();
        return Results.Ok(new { items = result, page = currentPage, pageSize = size, total });
    }

    private static string Fingerprint(IReadOnlyList<CommerceCheckoutLineInput> items)
    {
        var normalized = string.Join('\n', items.OrderBy(x => x.Slug, StringComparer.Ordinal)
            .Select(x => $"{x.Slug}\0{x.Quantity}"));
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(normalized)));
    }

    private static string CreateOrderNumber() => $"NH-D-{DateTimeOffset.UtcNow:yyyyMMdd}-{Convert.ToHexString(RandomNumberGenerator.GetBytes(6))}";

    private static bool UniqueViolationOrDeadlock(DbUpdateException exception) =>
        exception.InnerException is SqlException { Number: 1205 or 2601 or 2627 };

    private static async Task RollbackQuietlyAsync(Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction transaction, CancellationToken ct)
    {
        try { await transaction.RollbackAsync(ct); }
        catch (SqlException) { /* SQL Server may have already rolled back a deadlock victim. */ }
        catch (InvalidOperationException) { /* The server has already completed the transaction. */ }
    }

    private sealed record CommerceCheckoutRequest(IReadOnlyList<CommerceCheckoutLineInput>? Items);
}
