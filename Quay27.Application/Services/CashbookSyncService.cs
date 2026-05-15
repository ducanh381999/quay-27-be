using Quay27.Application.Abstractions;
using Quay27.Application.Cashbook;
using Quay27.Application.Common;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

public sealed class CashbookSyncService : ICashbookSyncService
{
    public const string SupplierPaymentAllocationSourceKind = "SupplierPaymentAllocation";

    private readonly ICashbookRepository _cashbook;
    private readonly IPaymentCategoryRepository _categories;
    private readonly ICurrentUser _currentUser;

    public CashbookSyncService(
        ICashbookRepository cashbook,
        IPaymentCategoryRepository categories,
        ICurrentUser currentUser)
    {
        _cashbook = cashbook;
        _categories = categories;
        _currentUser = currentUser;
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

        if (await _cashbook.ExistsEntryForSourceAsync("SalesReturn", salesReturn.Id, cancellationToken))
            return;

        var net = MoneyMath.Round(salesReturn.NetAmountDueFromCustomer);
        var refundDue = MoneyMath.Round(salesReturn.RefundDueAmount);

        var isReceipt = salesReturn.HasExchangeItems && net > 0;
        var isRefundPayout = !isReceipt && (refundDue > 0 || net < 0);
        if (!isReceipt && !isRefundPayout)
            return;

        var amount = isReceipt
            ? MoneyMath.Round(salesReturn.PaidAmount > 0 ? salesReturn.PaidAmount : net)
            : MoneyMath.Round(net < 0 ? Math.Abs(net) : refundDue);

        if (amount <= 0)
            return;

        var paymentMethod = isReceipt
            ? salesReturn.PaymentMethod ?? "cash"
            : salesReturn.RefundPaymentMethod ?? "cash";

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
            FundType = MapPaymentMethodToFundType(paymentMethod),
            OccurredAtUtc = DateTime.UtcNow,
            Amount = amount,
            Note = isReceipt
                ? $"Thu thêm trả hàng {salesReturn.Code}"
                : $"Hoàn tiền trả hàng {salesReturn.Code}",
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
            StaffUserId = salesReturn.ReceivedByUserId ?? salesReturn.SellerUserId,
            CreatedAtUtc = DateTime.UtcNow,
        };

        await _cashbook.AddEntryAsync(entry, cancellationToken);
    }

    public async Task SyncGoodsReceiptSupplierPaymentsAsync(GoodsReceipt receipt,
        IReadOnlyList<Guid> previousPaymentAllocationIds, CancellationToken cancellationToken = default)
    {
        foreach (var id in previousPaymentAllocationIds)
        {
            await _cashbook.RemoveEntriesBySourceAsync(SupplierPaymentAllocationSourceKind, id, cancellationToken);
        }

        if (!IsGoodsReceiptCompleted(receipt.Status))
            return;

        var collectorUserId = _currentUser.UserId
                              ?? throw new InvalidOperationException("Cần đăng nhập để ghi sổ quỹ.");
        var cat = await _categories.GetByCodeAsync("SupplierPurchasePayment", cancellationToken)
                  ?? throw new InvalidOperationException("Thiếu danh mục SupplierPurchasePayment (Chi Tiền trả NCC). Chạy migration.");

        var display = SupplierDisplayName(receipt.Supplier?.Code, receipt.Supplier?.Name);
        var occurredAtUtc = ToOccurredUtc(receipt.ReceiptDate);

        var positiveAllocations = receipt.PaymentAllocations
            .Where(a => MoneyMath.Round(a.Amount) > 0)
            .ToList();
        var allocCount = positiveAllocations.Count;

        for (var i = 0; i < positiveAllocations.Count; i++)
        {
            var alloc = positiveAllocations[i];
            var amount = MoneyMath.Round(alloc.Amount);
            var code = CashbookCodeFormatting.FormatGoodsReceiptSupplierPaymentCode(receipt.Code, i, allocCount);
            var entry = new CashbookEntry
            {
                Id = Guid.NewGuid(),
                Code = code,
                EntryType = "Payment",
                FundType = MapSupplierAllocationFundType(alloc.PaymentMethod),
                OccurredAtUtc = occurredAtUtc,
                Amount = amount,
                Note = $"Chi trả NCC — phiếu nhập {receipt.Code}",
                PaymentCategoryId = cat.Id,
                AffectsBusinessResult = false,
                Status = "paid",
                CollectorUserId = collectorUserId,
                CounterpartyScope = "supplier",
                CashbookPartyId = null,
                CounterpartyDisplayName = display,
                PartnerDebtMode = "not_applicable",
                SourceKind = SupplierPaymentAllocationSourceKind,
                SourceId = alloc.Id,
                CreatedByUserId = collectorUserId,
                StaffUserId = null,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _cashbook.AddEntryAsync(entry, cancellationToken);
        }
    }

    public async Task SyncReturnReceiptSupplierPaymentsAsync(ReturnReceipt receipt,
        IReadOnlyList<Guid> previousPaymentAllocationIds, CancellationToken cancellationToken = default)
    {
        foreach (var id in previousPaymentAllocationIds)
        {
            await _cashbook.RemoveEntriesBySourceAsync(SupplierPaymentAllocationSourceKind, id, cancellationToken);
        }

        if (!IsReturnReceiptCompleted(receipt.Status))
            return;

        var collectorUserId = _currentUser.UserId
                              ?? throw new InvalidOperationException("Cần đăng nhập để ghi sổ quỹ.");
        var cat = await _categories.GetByCodeAsync("SupplierReturnRefund", cancellationToken)
                  ?? throw new InvalidOperationException("Thiếu danh mục SupplierReturnRefund (Thu Tiền NCC hoàn trả). Chạy migration.");

        var display = SupplierDisplayName(receipt.Supplier?.Code, receipt.Supplier?.Name);
        var occurredAtUtc = ToOccurredUtc(receipt.ReturnDate);

        foreach (var alloc in receipt.PaymentAllocations)
        {
            var amount = MoneyMath.Round(alloc.Amount);
            if (amount <= 0)
                continue;

            var code = await _cashbook.GenerateNextReceiptCodeAsync("THN", cancellationToken);
            var entry = new CashbookEntry
            {
                Id = Guid.NewGuid(),
                Code = code,
                EntryType = "Receipt",
                FundType = MapSupplierAllocationFundType(alloc.PaymentMethod),
                OccurredAtUtc = occurredAtUtc,
                Amount = amount,
                Note = $"Thu tiền NCC hoàn trả — phiếu trả hàng nhập {receipt.Code}",
                PaymentCategoryId = cat.Id,
                AffectsBusinessResult = false,
                Status = "paid",
                CollectorUserId = collectorUserId,
                CounterpartyScope = "supplier",
                CashbookPartyId = null,
                CounterpartyDisplayName = display,
                PartnerDebtMode = "not_applicable",
                SourceKind = SupplierPaymentAllocationSourceKind,
                SourceId = alloc.Id,
                CreatedByUserId = collectorUserId,
                StaffUserId = null,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _cashbook.AddEntryAsync(entry, cancellationToken);
        }
    }

    private static bool IsGoodsReceiptCompleted(string? status) =>
        string.Equals(status?.Trim(), "received", StringComparison.OrdinalIgnoreCase);

    private static bool IsReturnReceiptCompleted(string? status) =>
        string.Equals(status?.Trim(), "returned", StringComparison.OrdinalIgnoreCase);

    private static string SupplierDisplayName(string? code, string? name)
    {
        var c = code?.Trim() ?? string.Empty;
        var n = name?.Trim() ?? string.Empty;
        if (c.Length > 0 && n.Length > 0)
            return $"{c} — {n}";
        if (n.Length > 0)
            return n;
        if (c.Length > 0)
            return c;
        return "Nhà cung cấp";
    }

    private static DateTime ToOccurredUtc(DateTime localOrUnspecified)
    {
        if (localOrUnspecified.Kind == DateTimeKind.Utc)
            return localOrUnspecified;
        if (localOrUnspecified.Kind == DateTimeKind.Local)
            return localOrUnspecified.ToUniversalTime();
        return DateTime.SpecifyKind(localOrUnspecified, DateTimeKind.Local).ToUniversalTime();
    }

    private static string MapSupplierAllocationFundType(string paymentMethod)
    {
        var m = paymentMethod?.Trim().ToLowerInvariant() ?? "cash";
        return m switch
        {
            "cash" => "cash",
            "card" => "bank",
            "banktransfer" => "bank",
            _ => "cash",
        };
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
