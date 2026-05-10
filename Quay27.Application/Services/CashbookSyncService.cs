using Quay27.Application.Abstractions;
using Quay27.Application.Common;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

public sealed class CashbookSyncService : ICashbookSyncService
{
    private readonly ICashbookRepository _cashbook;
    private readonly IPaymentCategoryRepository _categories;

    public CashbookSyncService(ICashbookRepository cashbook, IPaymentCategoryRepository categories)
    {
        _cashbook = cashbook;
        _categories = categories;
    }

    public async Task SyncAfterSalesInvoiceStatusAsync(SalesInvoice invoice, string? previousStatus,
        CancellationToken cancellationToken = default)
    {
        var prev = previousStatus?.Trim() ?? string.Empty;
        var now = invoice.Status?.Trim() ?? string.Empty;
        var wasCompleted = string.Equals(prev, "completed", StringComparison.OrdinalIgnoreCase);
        var isCompleted = string.Equals(now, "completed", StringComparison.OrdinalIgnoreCase);

        if (wasCompleted && !isCompleted)
        {
            await _cashbook.RemoveEntriesBySourceAsync("SalesInvoice", invoice.Id, cancellationToken);
            return;
        }

        if (!isCompleted || invoice.PaidAmount <= 0)
            return;

        if (await _cashbook.ExistsEntryForSourceAsync("SalesInvoice", invoice.Id, cancellationToken))
            return;

        var cat = await _categories.GetByCodeAsync("CustomerPayment", cancellationToken)
                  ?? throw new InvalidOperationException("Payment category CustomerPayment is missing.");

        var code = await _cashbook.GenerateNextReceiptCodeAsync("SalesInvoice", cancellationToken);
        var fund = MapPaymentMethodToFundType(invoice.PaymentMethod);
        var display = string.IsNullOrWhiteSpace(invoice.CustomerName)
            ? invoice.CustomerCode
            : invoice.CustomerName?.Trim();

        var entry = new CashbookEntry
        {
            Id = Guid.NewGuid(),
            Code = code,
            EntryType = "Receipt",
            FundType = fund,
            OccurredAtUtc = DateTime.UtcNow,
            Amount = MoneyMath.Round(invoice.PaidAmount),
            Note = $"Thu tiền từ hóa đơn {invoice.Code}",
            PaymentCategoryId = cat.Id,
            AffectsBusinessResult = false,
            Status = "paid",
            CollectorUserId = invoice.CreatedByUserId ?? invoice.SellerUserId
                              ?? throw new InvalidOperationException("Hóa đơn thiếu người tạo/người bán để ghi sổ quỹ."),
            CounterpartyScope = "customer",
            CashbookPartyId = null,
            CounterpartyDisplayName = display,
            PartnerDebtMode = "not_applicable",
            SourceKind = "SalesInvoice",
            SourceId = invoice.Id,
            CreatedByUserId = invoice.CreatedByUserId,
            StaffUserId = invoice.SellerUserId,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _cashbook.AddEntryAsync(entry, cancellationToken);
    }

    public async Task SyncAfterPurchaseOrderStatusAsync(PurchaseOrder order, string? previousStatus,
        CancellationToken cancellationToken = default)
    {
        var prev = previousStatus?.Trim() ?? string.Empty;
        var now = order.Status?.Trim() ?? string.Empty;
        var wasCompleted = string.Equals(prev, "completed", StringComparison.OrdinalIgnoreCase);
        var isCompleted = string.Equals(now, "completed", StringComparison.OrdinalIgnoreCase);

        if (wasCompleted && !isCompleted)
        {
            await _cashbook.RemoveEntriesBySourceAsync("PurchaseOrder", order.Id, cancellationToken);
            return;
        }

        if (!isCompleted || order.AmountPaid <= 0)
            return;

        if (await _cashbook.ExistsEntryForSourceAsync("PurchaseOrder", order.Id, cancellationToken))
            return;

        var cat = await _categories.GetByCodeAsync("CustomerPayment", cancellationToken)
                  ?? throw new InvalidOperationException("Payment category CustomerPayment is missing.");

        var code = await _cashbook.GenerateNextReceiptCodeAsync("PurchaseOrder", cancellationToken);
        var fund = MapPaymentMethodToFundType(order.PaymentMethod);
        var display = string.IsNullOrWhiteSpace(order.CustomerName)
            ? order.CustomerCode
            : order.CustomerName?.Trim();

        var entry = new CashbookEntry
        {
            Id = Guid.NewGuid(),
            Code = code,
            EntryType = "Receipt",
            FundType = fund,
            OccurredAtUtc = DateTime.UtcNow,
            Amount = MoneyMath.Round(order.AmountPaid),
            Note = $"Thu tiền từ đặt hàng {order.Code}",
            PaymentCategoryId = cat.Id,
            AffectsBusinessResult = false,
            Status = "paid",
            CollectorUserId = order.CreatedByUserId
                              ?? throw new InvalidOperationException("Đặt hàng thiếu người tạo để ghi sổ quỹ."),
            CounterpartyScope = "customer",
            CashbookPartyId = null,
            CounterpartyDisplayName = display,
            PartnerDebtMode = "not_applicable",
            SourceKind = "PurchaseOrder",
            SourceId = order.Id,
            CreatedByUserId = order.CreatedByUserId,
            StaffUserId = order.SellerUserId,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _cashbook.AddEntryAsync(entry, cancellationToken);
    }

    public async Task SyncAfterSalesReturnStatusAsync(SalesReturn salesReturn, string? previousStatus,
        CancellationToken cancellationToken = default)
    {
        var prev = previousStatus?.Trim() ?? string.Empty;
        var now = salesReturn.Status?.Trim() ?? string.Empty;
        var wasReturned = string.Equals(prev, "returned", StringComparison.OrdinalIgnoreCase);
        var isReturned = string.Equals(now, "returned", StringComparison.OrdinalIgnoreCase);

        if (wasReturned && !isReturned)
        {
            await _cashbook.RemoveEntriesBySourceAsync("SalesReturn", salesReturn.Id, cancellationToken);
            return;
        }

        if (!isReturned)
            return;

        var net = MoneyMath.Round(salesReturn.NetAmountDueFromCustomer);
        if (net == 0)
            return;

        if (await _cashbook.ExistsEntryForSourceAsync("SalesReturn", salesReturn.Id, cancellationToken))
            return;

        var isReceipt = net > 0;
        var amount = MoneyMath.Round(Math.Abs(net));
        var customerCat = await _categories.GetByCodeAsync("CustomerPayment", cancellationToken)
                         ?? throw new InvalidOperationException("Payment category CustomerPayment is missing.");
        var expenseCat = await _categories.GetByCodeAsync("OtherExpense", cancellationToken)
                         ?? throw new InvalidOperationException("Payment category OtherExpense is missing.");

        var code = isReceipt
            ? await _cashbook.GenerateNextReceiptCodeAsync("SalesReturn", cancellationToken)
            : await _cashbook.GenerateNextPaymentCodeAsync(cancellationToken);
        var display = string.IsNullOrWhiteSpace(salesReturn.CustomerName)
            ? salesReturn.CustomerCode
            : salesReturn.CustomerName?.Trim();

        var entry = new CashbookEntry
        {
            Id = Guid.NewGuid(),
            Code = code,
            EntryType = isReceipt ? "Receipt" : "Payment",
            FundType = "cash",
            OccurredAtUtc = DateTime.UtcNow,
            Amount = amount,
            Note = $"Trả hàng {salesReturn.Code} (NetAmountDueFromCustomer)",
            PaymentCategoryId = isReceipt ? customerCat.Id : expenseCat.Id,
            AffectsBusinessResult = false,
            Status = "paid",
            CollectorUserId = salesReturn.CreatedByUserId
                              ?? throw new InvalidOperationException("Trả hàng thiếu người tạo để ghi sổ quỹ."),
            CounterpartyScope = "customer",
            CashbookPartyId = null,
            CounterpartyDisplayName = display,
            PartnerDebtMode = "not_applicable",
            SourceKind = "SalesReturn",
            SourceId = salesReturn.Id,
            CreatedByUserId = salesReturn.CreatedByUserId,
            StaffUserId = salesReturn.ReceivedByUserId,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _cashbook.AddEntryAsync(entry, cancellationToken);
    }

    private static string MapPaymentMethodToFundType(string paymentMethod)
    {
        var m = paymentMethod?.Trim().ToLowerInvariant() ?? "cash";
        return m switch
        {
            "cash" => "cash",
            "wallet" => "ewallet",
            "transfer" => "bank",
            "card" => "bank",
            "banktransfer" => "bank",
            _ => "cash",
        };
    }
}
