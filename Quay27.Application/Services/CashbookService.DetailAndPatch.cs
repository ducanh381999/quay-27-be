using FluentValidation;
using FluentValidation.Results;
using Quay27.Application.Cashbook;
using Quay27.Application.Common;
using Quay27.Application.Common.Exceptions;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

public sealed partial class CashbookService
{
    public async Task<CashbookEntryDetailDto> GetEntryDetailAsync(Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var entry = await _cashbook.GetEntryForReadAsync(id, cancellationToken);
        if (entry is null)
            throw new NotFoundException("Không tìm thấy phiếu sổ quỹ.");

        var (allocations, sourceSummary) = await BuildAllocationsAndSummaryAsync(entry, cancellationToken);
        var canCancel = string.Equals(entry.SourceKind, SkManual, StringComparison.OrdinalIgnoreCase) &&
                        string.Equals(entry.Status, "paid", StringComparison.OrdinalIgnoreCase);
        var canEditAlloc = CanEditAllocations(entry);
        return MapToDetailDto(entry, allocations, sourceSummary, canCancel, canEditAlloc);
    }

    public async Task PatchEntryAsync(Guid id, PatchCashbookEntryRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var vr = await _patchValidator.ValidateAsync(request, cancellationToken);
        if (!vr.IsValid)
            throw new ValidationException(vr.Errors);

        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entry = await _cashbook.GetEntryTrackedAsync(id, cancellationToken);
            if (entry is null)
                throw new NotFoundException("Không tìm thấy phiếu sổ quỹ.");
            if (!string.Equals(entry.Status, "paid", StringComparison.OrdinalIgnoreCase))
                throw new ConflictException("Phiếu đã hủy — không thể sửa.");

            if (request.PaymentCategoryId.HasValue)
            {
                var cat = await _categories.GetByIdAsync(request.PaymentCategoryId.Value, cancellationToken)
                          ?? throw new ValidationException(new[]
                          {
                              new ValidationFailure(nameof(request.PaymentCategoryId), "Danh mục không tồn tại."),
                          });
                var expect = string.Equals(entry.EntryType, "Receipt", StringComparison.OrdinalIgnoreCase)
                    ? "Receipt"
                    : "Expense";
                if (!string.Equals(cat.Kind, expect, StringComparison.OrdinalIgnoreCase))
                {
                    throw new ValidationException(new[]
                    {
                        new ValidationFailure(nameof(request.PaymentCategoryId), "Danh mục không khớp loại phiếu."),
                    });
                }

                entry.PaymentCategoryId = cat.Id;
            }

            if (request.OccurredAtUtc.HasValue)
                entry.OccurredAtUtc = request.OccurredAtUtc.Value.Kind == DateTimeKind.Unspecified
                    ? DateTime.SpecifyKind(request.OccurredAtUtc.Value, DateTimeKind.Utc)
                    : request.OccurredAtUtc.Value.ToUniversalTime();

            if (request.CollectorUserId.HasValue)
            {
                await EnsureCollectorExistsAsync(request.CollectorUserId.Value, cancellationToken);
                entry.CollectorUserId = request.CollectorUserId.Value;
            }

            if (request.StaffUserId.HasValue)
                entry.StaffUserId = request.StaffUserId;

            if (request.AffectsBusinessResult.HasValue)
                entry.AffectsBusinessResult = request.AffectsBusinessResult.Value;

            if (!string.IsNullOrWhiteSpace(request.FundType))
                entry.FundType = request.FundType.Trim().ToLowerInvariant();

            if (request.Note != null)
                entry.Note = string.IsNullOrWhiteSpace(request.Note) ? null : request.Note.Trim();

            var isManual = string.Equals(entry.SourceKind, SkManual, StringComparison.OrdinalIgnoreCase);
            if (isManual)
            {
                if (!string.IsNullOrWhiteSpace(request.CounterpartyScope))
                    entry.CounterpartyScope = request.CounterpartyScope.Trim().ToLowerInvariant();

                if (request.CashbookPartyId.HasValue || request.CounterpartyDisplayName != null)
                {
                    var (name, partyId) = await ResolveCounterpartyForPatchAsync(
                        request.CashbookPartyId,
                        request.CounterpartyDisplayName,
                        cancellationToken);
                    entry.CashbookPartyId = partyId;
                    entry.CounterpartyDisplayName = name;
                }
            }

            if (request.Allocations is { Count: > 0 })
                await ApplyAllocationPatchAsync(entry, request.Allocations, cancellationToken);
            else if (request.Amount.HasValue)
            {
                if (!isManual)
                    throw new ConflictException("Chỉ có thể đổi trực tiếp số tiền trên phiếu thủ công — dùng phân bổ cho phiếu liên kết.");
                entry.Amount = MoneyMath.Round(request.Amount.Value);
            }

            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public async Task CancelEntryAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entry = await _cashbook.GetEntryTrackedAsync(id, cancellationToken);
            if (entry is null)
                throw new NotFoundException("Không tìm thấy phiếu sổ quỹ.");
            if (!string.Equals(entry.Status, "paid", StringComparison.OrdinalIgnoreCase))
                throw new ConflictException("Phiếu đã được hủy trước đó.");
            if (!string.Equals(entry.SourceKind, SkManual, StringComparison.OrdinalIgnoreCase))
                throw new ConflictException("Phiếu tự động không thể hủy từ sổ quỹ — điều chỉnh trên chứng từ gốc.");

            entry.Status = "cancelled";
            await _unitOfWork.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    private static bool CanEditAllocations(CashbookEntry entry) =>
        string.Equals(entry.Status, "paid", StringComparison.OrdinalIgnoreCase) &&
        entry.SourceId.HasValue &&
        (string.Equals(entry.SourceKind, SkSalesInvoice, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(entry.SourceKind, SkSupplierPaymentAllocation, StringComparison.OrdinalIgnoreCase) ||
         string.Equals(entry.SourceKind, "PurchaseOrder", StringComparison.OrdinalIgnoreCase));

    private CashbookEntryDetailDto MapToDetailDto(
        CashbookEntry entry,
        IReadOnlyList<CashbookEntryAllocationLineDto> allocations,
        string? sourceSummary,
        bool canCancel,
        bool canEditAlloc) =>
        new(
            entry.Id,
            entry.Code,
            entry.EntryType,
            entry.FundType,
            entry.OccurredAtUtc,
            entry.Amount,
            entry.Note,
            entry.PaymentCategoryId,
            entry.PaymentCategory?.Name,
            entry.PaymentCategory?.Code,
            entry.AffectsBusinessResult,
            entry.Status,
            entry.CollectorUserId,
            DispName(entry.CollectorUser),
            entry.CreatedByUserId,
            DispName(entry.CreatedByUser),
            entry.StaffUserId,
            DispName(entry.StaffUser),
            entry.CounterpartyScope,
            entry.CashbookPartyId,
            entry.CounterpartyDisplayName,
            entry.PartnerDebtMode,
            entry.SourceKind,
            entry.SourceId,
            sourceSummary,
            allocations,
            CanEditHeaderFields: string.Equals(entry.Status, "paid", StringComparison.OrdinalIgnoreCase),
            canEditAlloc,
            canCancel);

    private static string? DispName(User? u) =>
        u is null ? null : (string.IsNullOrWhiteSpace(u.FullName) ? u.Username : u.FullName.Trim());

    private async Task<(IReadOnlyList<CashbookEntryAllocationLineDto> Lines, string? Summary)>
        BuildAllocationsAndSummaryAsync(CashbookEntry entry, CancellationToken cancellationToken)
    {
        if (!entry.SourceId.HasValue)
            return (Array.Empty<CashbookEntryAllocationLineDto>(), null);

        if (string.Equals(entry.SourceKind, SkSalesInvoice, StringComparison.OrdinalIgnoreCase))
        {
            var inv = await _salesInvoices.GetByIdNoTrackingAsync(entry.SourceId.Value, cancellationToken);
            if (inv is null)
                return (Array.Empty<CashbookEntryAllocationLineDto>(), null);
            var maxDue = MoneyMath.Round(inv.SubtotalAmount - inv.DiscountAmount);
            var prev = MoneyMath.Round(Math.Max(0m, inv.PaidAmount - entry.Amount));
            var line = new CashbookEntryAllocationLineDto(
                inv.Id,
                inv.Code,
                inv.CreatedAtUtc,
                maxDue,
                prev,
                entry.Amount,
                inv.Status);
            return ([line], $"Phiếu thu tự động được gắn với hóa đơn {inv.Code}");
        }

        if (string.Equals(entry.SourceKind, "SalesReturn", StringComparison.OrdinalIgnoreCase))
        {
            var sr = await _salesReturns.GetByIdNoTrackingAsync(entry.SourceId.Value, cancellationToken);
            if (sr is null)
                return (Array.Empty<CashbookEntryAllocationLineDto>(), null);
            var docTotal = MoneyMath.Round(Math.Abs(sr.NetAmountDueFromCustomer) > 0
                ? Math.Abs(sr.NetAmountDueFromCustomer)
                : sr.RefundDueAmount);
            var line = new CashbookEntryAllocationLineDto(
                sr.Id,
                sr.Code,
                sr.CreatedAtUtc,
                docTotal,
                0m,
                entry.Amount,
                sr.Status);
            return ([line], $"Phiếu tự động được gắn với đơn trả hàng {sr.Code}");
        }

        if (string.Equals(entry.SourceKind, "PurchaseOrder", StringComparison.OrdinalIgnoreCase))
        {
            var po = await _purchaseOrders.GetByIdNoTrackingAsync(entry.SourceId.Value, cancellationToken);
            if (po is null)
                return (Array.Empty<CashbookEntryAllocationLineDto>(), null);
            var docTotal = MoneyMath.Round(po.SubtotalAmount - po.DiscountAmount);
            var prev = MoneyMath.Round(Math.Max(0m, po.AmountPaid - entry.Amount));
            var line = new CashbookEntryAllocationLineDto(
                po.Id,
                po.Code,
                po.CreatedAtUtc,
                docTotal,
                prev,
                entry.Amount,
                po.Status);
            return ([line], $"Phiếu thu tự động được gắn với đơn đặt hàng {po.Code}");
        }

        if (string.Equals(entry.SourceKind, SkSupplierPaymentAllocation, StringComparison.OrdinalIgnoreCase))
        {
            var alloc = await _cashbook.GetSupplierPaymentAllocationForReadAsync(entry.SourceId.Value, cancellationToken);
            if (alloc is null)
                return (Array.Empty<CashbookEntryAllocationLineDto>(), null);

            if (alloc.GoodsReceiptId.HasValue && alloc.GoodsReceipt is not null)
            {
                var gr = alloc.GoodsReceipt;
                var others = gr.PaymentAllocations.Where(a => a.Id != alloc.Id).Sum(a => a.Amount);
                var prev = MoneyMath.Round(others);
                var line = new CashbookEntryAllocationLineDto(
                    alloc.Id,
                    gr.Code,
                    gr.ReceiptDate.ToUniversalTime(),
                    MoneyMath.Round(gr.Total),
                    prev,
                    entry.Amount,
                    gr.Status);
                var isPayment = string.Equals(entry.EntryType, "Payment", StringComparison.OrdinalIgnoreCase);
                var summary = isPayment
                    ? $"Phiếu chi tự động được gắn với phiếu nhập {gr.Code}"
                    : $"Phiếu thu tự động được gắn với phiếu nhập {gr.Code}";
                return ([line], summary);
            }

            if (alloc.ReturnReceiptId.HasValue && alloc.ReturnReceipt is not null)
            {
                var rr = alloc.ReturnReceipt;
                var others = rr.PaymentAllocations.Where(a => a.Id != alloc.Id).Sum(a => a.Amount);
                var prev = MoneyMath.Round(others);
                var line = new CashbookEntryAllocationLineDto(
                    alloc.Id,
                    rr.Code,
                    rr.ReturnDate.ToUniversalTime(),
                    MoneyMath.Round(rr.Total),
                    prev,
                    entry.Amount,
                    rr.Status);
                return ([line], $"Phiếu thu tự động được gắn với đơn trả hàng nhập {rr.Code}");
            }
        }

        return (Array.Empty<CashbookEntryAllocationLineDto>(), $"Nguồn: {entry.SourceKind}");
    }

    private async Task ApplyAllocationPatchAsync(CashbookEntry entry,
        IReadOnlyList<PatchCashbookEntryAllocationItem> items,
        CancellationToken cancellationToken)
    {
        if (items.Count != 1)
            throw new ValidationException(new[]
                { new ValidationFailure(nameof(items), "Hiện chỉ hỗ trợ một dòng phân bổ.") });

        var item = items[0];
        var amt = MoneyMath.Round(item.Amount);

        if (string.Equals(entry.SourceKind, SkSalesInvoice, StringComparison.OrdinalIgnoreCase))
        {
            if (!entry.SourceId.HasValue || item.TargetId != entry.SourceId.Value)
                throw new ValidationException(new[] { new ValidationFailure(nameof(item.TargetId), "Sai chứng từ phân bổ.") });

            var inv = await _salesInvoices.GetTrackedByIdAsync(item.TargetId, cancellationToken)
                      ?? throw new NotFoundException("Không tìm thấy hóa đơn.");
            if (!string.Equals(inv.Status, "completed", StringComparison.OrdinalIgnoreCase))
                throw new ConflictException("Chỉ sửa phân bổ khi hóa đơn ở trạng thái hoàn thành.");

            var maxDue = MoneyMath.Round(inv.SubtotalAmount - inv.DiscountAmount);
            if (amt < 0m || amt > maxDue)
                throw new ValidationException(new[]
                    { new ValidationFailure(nameof(item.Amount), "Số tiền phân bổ không hợp lệ so với hóa đơn.") });

            inv.PaidAmount = amt;
            entry.Amount = amt;
            return;
        }

        if (string.Equals(entry.SourceKind, "PurchaseOrder", StringComparison.OrdinalIgnoreCase))
        {
            if (!entry.SourceId.HasValue || item.TargetId != entry.SourceId.Value)
                throw new ValidationException(new[] { new ValidationFailure(nameof(item.TargetId), "Sai chứng từ phân bổ.") });

            var po = await _purchaseOrders.GetTrackedByIdAsync(item.TargetId, cancellationToken)
                     ?? throw new NotFoundException("Không tìm thấy đơn đặt hàng.");
            if (!string.Equals(po.Status, "completed", StringComparison.OrdinalIgnoreCase))
                throw new ConflictException("Chỉ sửa phân bổ khi đơn đặt hàng ở trạng thái hoàn thành.");

            var maxPay = MoneyMath.Round(po.SubtotalAmount - po.DiscountAmount);
            if (amt < 0m || amt > maxPay)
                throw new ValidationException(new[]
                    { new ValidationFailure(nameof(item.Amount), "Số tiền phân bổ không hợp lệ so với đơn đặt hàng.") });

            po.AmountPaid = amt;
            entry.Amount = amt;
            return;
        }

        if (string.Equals(entry.SourceKind, SkSupplierPaymentAllocation, StringComparison.OrdinalIgnoreCase))
        {
            if (!entry.SourceId.HasValue || item.TargetId != entry.SourceId.Value)
                throw new ValidationException(new[] { new ValidationFailure(nameof(item.TargetId), "Sai dòng phân bổ.") });

            var alloc = await _cashbook.GetSupplierPaymentAllocationTrackedAsync(item.TargetId, cancellationToken)
                        ?? throw new NotFoundException("Không tìm thấy phân bổ thanh toán.");

            if (amt <= 0m)
                throw new ValidationException(new[]
                    { new ValidationFailure(nameof(item.Amount), "Số tiền phải lớn hơn 0.") });

            alloc.Amount = amt;
            entry.Amount = amt;

            if (alloc.GoodsReceipt is not null)
            {
                var gr = alloc.GoodsReceipt;
                gr.PaidAmount = MoneyMath.Round(gr.PaymentAllocations.Sum(a => a.Amount));
                gr.SupplierDebtDelta =
                    MoneyMath.Round(Math.Max(0m, gr.Total - gr.PaidAmount - gr.SupplierPayableDiscountPortion));
            }
            else if (alloc.ReturnReceipt is not null)
            {
                var rr = alloc.ReturnReceipt;
                rr.SupplierPaidAmount = MoneyMath.Round(rr.PaymentAllocations.Sum(a => a.Amount));
                rr.SupplierDebtDelta = MoneyMath.Round(Math.Max(0m, rr.Total - rr.SupplierPaidAmount));
            }

            return;
        }

        throw new ConflictException("Loại nguồn này chưa hỗ trợ sửa phân bổ.");
    }

    private async Task<(string? DisplayName, Guid? PartyId)> ResolveCounterpartyForPatchAsync(
        Guid? cashbookPartyId,
        string? counterpartyDisplayName,
        CancellationToken cancellationToken)
    {
        if (cashbookPartyId.HasValue)
        {
            var party = await _cashbook.GetPartyByIdAsync(cashbookPartyId.Value, cancellationToken)
                        ?? throw new ValidationException(new[]
                        {
                            new ValidationFailure(nameof(cashbookPartyId), "Người nộp/nhận không tồn tại."),
                        });

            var name = string.IsNullOrWhiteSpace(counterpartyDisplayName)
                ? party.Name
                : counterpartyDisplayName.Trim();
            return (name, party.Id);
        }

        if (string.IsNullOrWhiteSpace(counterpartyDisplayName))
        {
            throw new ValidationException(new[]
            {
                new ValidationFailure(nameof(counterpartyDisplayName), "Nhập tên người nộp/nhận."),
            });
        }

        return (counterpartyDisplayName.Trim(), null);
    }
}
