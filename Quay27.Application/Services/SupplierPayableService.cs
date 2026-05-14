using Microsoft.Extensions.Logging;
using Quay27.Application.Abstractions;
using Quay27.Application.Cashbook;
using Quay27.Application.Common;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.Repositories;
using Quay27.Application.Suppliers;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

public sealed class SupplierPayableService : ISupplierPayableService
{
    private readonly ISupplierPayableRepository _payables;
    private readonly ISupplierRepository _suppliers;
    private readonly IGoodsReceiptRepository _goodsReceipts;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUser _currentUser;
    private readonly ICashbookRepository _cashbook;
    private readonly IPaymentCategoryRepository _paymentCategories;
    private readonly IReceivingAccountRepository _receivingAccounts;
    private readonly IUserRepository _users;
    private readonly ILogger<SupplierPayableService> _logger;

    public SupplierPayableService(
        ISupplierPayableRepository payables,
        ISupplierRepository suppliers,
        IGoodsReceiptRepository goodsReceipts,
        IUnitOfWork unitOfWork,
        ICurrentUser currentUser,
        ICashbookRepository cashbook,
        IPaymentCategoryRepository paymentCategories,
        IReceivingAccountRepository receivingAccounts,
        IUserRepository users,
        ILogger<SupplierPayableService> logger)
    {
        _payables = payables;
        _suppliers = suppliers;
        _goodsReceipts = goodsReceipts;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _cashbook = cashbook;
        _paymentCategories = paymentCategories;
        _receivingAccounts = receivingAccounts;
        _users = users;
        _logger = logger;
    }

    public Task<IReadOnlyList<SupplierPayableTransactionDto>> ListTransactionsAsync(
        Guid supplierId,
        int? transactionType,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        return _payables.ListTransactionsAsync(supplierId, transactionType, cancellationToken);
    }

    public Task<IReadOnlyList<SupplierOpenGoodsReceiptRowDto>> ListOpenGoodsReceiptsAsync(
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        return _payables.ListOpenGoodsReceiptsAsync(supplierId, cancellationToken);
    }

    public async Task<byte[]> ExportTransactionsExcelAsync(
        Guid supplierId,
        int? transactionType,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var supplier = await _suppliers.GetProjectedByIdAsync(supplierId, cancellationToken)
            ?? throw new NotFoundException("Nhà cung cấp không tồn tại.");
        var rows = await _payables.ListTransactionsAsync(supplierId, transactionType, cancellationToken);
        return SupplierPayableExcelExporter.BuildTransactionsReport(
            supplier.Code,
            supplier.Name,
            rows,
            DateTime.UtcNow);
    }

    public async Task<byte[]> ExportSupplierDebtSnapshotExcelAsync(Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var supplier = await _suppliers.GetProjectedByIdAsync(supplierId, cancellationToken)
            ?? throw new NotFoundException("Nhà cung cấp không tồn tại.");
        return SupplierPayableExcelExporter.BuildSupplierDebtSnapshot(
            supplier.Code,
            supplier.Name,
            supplier.CurrentDebt,
            DateTime.UtcNow);
    }

    public Task<SupplierDebtAdjustmentDto> CreateAdjustmentAsync(
        Guid supplierId,
        CreateSupplierDebtAdjustmentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var delta = MoneyMath.Round(request.Delta);
        if (delta == 0m)
            throw new ConflictException("Giá trị điều chỉnh phải khác 0.");

        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var supplier = await _suppliers.GetTrackedAsync(supplierId, cancellationToken)
                ?? throw new NotFoundException("Nhà cung cấp không tồn tại.");

            var newDebt = MoneyMath.Round(supplier.CurrentDebt + delta);
            if (newDebt < 0m)
                throw new ConflictException("Nợ sau điều chỉnh không được âm.");

            supplier.CurrentDebt = newDebt;

            var entity = new SupplierDebtAdjustment
            {
                Id = Guid.NewGuid(),
                Code = await _payables.GenerateNextAdjustmentCodeAsync(cancellationToken),
                SupplierId = supplierId,
                OccurredAtUtc = request.OccurredAtUtc ?? DateTime.UtcNow,
                Delta = delta,
                Description = string.IsNullOrWhiteSpace(request.Description) ? string.Empty : request.Description.Trim(),
                CreatedBy = _currentUser.Username,
                CreatedAtUtc = DateTime.UtcNow,
            };

            await _payables.AddAdjustmentAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Supplier {SupplierId} debt adjustment {Code} delta {Delta}", supplierId, entity.Code,
                delta);

            return new SupplierDebtAdjustmentDto(entity.Id, entity.Code, entity.OccurredAtUtc, entity.Delta,
                entity.Description);
        }, cancellationToken);
    }

    public Task<SupplierPayablePaymentDto> CreatePaymentAsync(
        Guid supplierId,
        CreateSupplierPayablePaymentRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var amount = MoneyMath.Round(request.Amount);
            if (amount <= 0m)
                throw new ConflictException("Số tiền thanh toán phải lớn hơn 0.");

            if (await _users.GetByIdAsync(request.PayerUserId, cancellationToken) is null)
                throw new NotFoundException("Người chi không tồn tại.");

            var method = (request.PaymentMethod ?? "cash").Trim().ToLowerInvariant();
            if (method is "card" or "banktransfer")
            {
                if (!request.ReceivingAccountId.HasValue)
                    throw new ConflictException("Vui lòng chọn số tài khoản cho phương thức thẻ/chuyển khoản.");
                if (await _receivingAccounts.GetByIdAsync(request.ReceivingAccountId.Value, cancellationToken) is null)
                    throw new NotFoundException("Tài khoản nhận không tồn tại.");
            }

            if (request.PostToCashbook && (!request.CashbookPaymentCategoryId.HasValue ||
                                            request.CashbookPaymentCategoryId.Value <= 0))
                throw new ConflictException("Vui lòng chọn danh mục chi sổ quỹ khi ghi sổ quỹ.");

            var lines = request.Lines ?? Array.Empty<SupplierPayablePaymentLineRequest>();
            if (request.AllocateToDocuments)
            {
                if (lines.Count == 0)
                    throw new ConflictException("Vui lòng nhập phân bổ phiếu nhập.");
                var sum = MoneyMath.Round(lines.Sum(x => MoneyMath.Round(x.Amount)));
                if (!MoneyMath.EqualsMoney(sum, amount))
                    throw new ConflictException("Tổng phân bổ phải bằng số tiền thanh toán.");
            }
            else if (lines.Count > 0)
            {
                throw new ConflictException("Không gửi dòng phân bổ khi không bật phân bổ.");
            }

            var supplier = await _suppliers.GetTrackedAsync(supplierId, cancellationToken)
                ?? throw new NotFoundException("Nhà cung cấp không tồn tại.");

            if (amount > supplier.CurrentDebt)
                throw new ConflictException("Số tiền vượt nợ hiện tại.");

            if (request.AllocateToDocuments)
            {
                foreach (var line in lines)
                {
                    if (!line.GoodsReceiptId.HasValue)
                        throw new ConflictException("Thiếu mã phiếu nhập trong phân bổ.");
                    var gr = await _goodsReceipts.GetTrackedAsync(line.GoodsReceiptId.Value, cancellationToken)
                        ?? throw new NotFoundException($"Phiếu nhập {line.GoodsReceiptId} không tồn tại.");
                    if (gr.SupplierId != supplierId)
                        throw new ConflictException($"Phiếu {gr.Code} không thuộc nhà cung cấp này.");
                    var pay = MoneyMath.Round(line.Amount);
                    if (pay <= 0m)
                        throw new ConflictException("Số tiền phân bổ phải dương.");
                    if (pay > gr.SupplierDebtDelta)
                        throw new ConflictException($"Phân bổ vượt nợ còn lại của phiếu {gr.Code}.");
                }

                foreach (var line in lines)
                {
                    var gr = (await _goodsReceipts.GetTrackedAsync(line.GoodsReceiptId!.Value, cancellationToken))!;
                    var pay = MoneyMath.Round(line.Amount);
                    gr.PaidAmount = MoneyMath.Round(gr.PaidAmount + pay);
                    gr.SupplierDebtDelta =
                        MoneyMath.Round(Math.Max(0m, gr.Total - gr.PaidAmount - gr.SupplierPayableDiscountPortion));
                }
            }

            supplier.CurrentDebt = MoneyMath.Round(Math.Max(0m, supplier.CurrentDebt - amount));

            var payment = new SupplierPayablePayment
            {
                Id = Guid.NewGuid(),
                Code = await _payables.GenerateNextPaymentCodeAsync(cancellationToken),
                SupplierId = supplierId,
                OccurredAtUtc = request.OccurredAtUtc ?? DateTime.UtcNow,
                PayerUserId = request.PayerUserId,
                PaymentMethod = method,
                ReceivingAccountId = request.ReceivingAccountId,
                TotalAmount = amount,
                Note = request.Note?.Trim(),
                AllocateToDocuments = request.AllocateToDocuments,
                PostToCashbook = request.PostToCashbook,
            };

            foreach (var line in lines)
            {
                payment.Lines.Add(new SupplierPayablePaymentLine
                {
                    Id = Guid.NewGuid(),
                    PaymentId = payment.Id,
                    GoodsReceiptId = line.GoodsReceiptId,
                    Amount = MoneyMath.Round(line.Amount),
                });
            }

            if (request.PostToCashbook)
            {
                var category = await _paymentCategories.GetByIdAsync(request.CashbookPaymentCategoryId!.Value,
                    cancellationToken)
                    ?? throw new NotFoundException("Danh mục chi không tồn tại.");
                if (!string.Equals(category.Kind, "Expense", StringComparison.OrdinalIgnoreCase))
                    throw new ConflictException("Danh mục không phải loại chi.");

                var fundType = method switch
                {
                    "cash" => "cash",
                    "card" or "banktransfer" => "bank",
                    _ => "cash",
                };

                var supplierDto = await _suppliers.GetProjectedByIdAsync(supplierId, cancellationToken);
                var displayName = supplierDto?.Name ?? "Nhà cung cấp";

                var entry = new CashbookEntry
                {
                    Id = Guid.NewGuid(),
                    Code = await _cashbook.GenerateNextPaymentCodeAsync(cancellationToken),
                    EntryType = "Payment",
                    FundType = fundType,
                    OccurredAtUtc = payment.OccurredAtUtc,
                    Amount = amount,
                    Note = payment.Note,
                    PaymentCategoryId = category.Id,
                    AffectsBusinessResult = true,
                    Status = "paid",
                    CollectorUserId = request.PayerUserId,
                    CounterpartyScope = "supplier",
                    CashbookPartyId = null,
                    CounterpartyDisplayName = displayName,
                    PartnerDebtMode = "not_applicable",
                    SourceKind = "SupplierPayablePayment",
                    SourceId = payment.Id,
                    CreatedByUserId = _currentUser.UserId,
                    StaffUserId = request.PayerUserId,
                    CreatedAtUtc = DateTime.UtcNow,
                };

                await _cashbook.AddEntryAsync(entry, cancellationToken);
                payment.CashbookEntryId = entry.Id;
            }

            await _payables.AddPaymentAsync(payment, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Supplier {SupplierId} payment {Code} amount {Amount}", supplierId, payment.Code,
                amount);

            return new SupplierPayablePaymentDto(payment.Id, payment.Code, payment.OccurredAtUtc, payment.TotalAmount,
                payment.PostToCashbook);
        }, cancellationToken);
    }

    public Task<SupplierPayableDiscountDto> CreateDiscountAsync(
        Guid supplierId,
        CreateSupplierPayableDiscountRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var amount = MoneyMath.Round(request.Amount);
            if (amount <= 0m)
                throw new ConflictException("Chiết khấu phải lớn hơn 0.");

            if (await _users.GetByIdAsync(request.PerformerUserId, cancellationToken) is null)
                throw new NotFoundException("Người thực hiện không tồn tại.");

            var lines = request.Lines ?? Array.Empty<SupplierPayableDiscountLineRequest>();
            if (request.AllocateToDocuments)
            {
                if (lines.Count == 0)
                    throw new ConflictException("Vui lòng nhập phân bổ chiết khấu.");
                var sum = MoneyMath.Round(lines.Sum(x => MoneyMath.Round(x.Amount)));
                if (!MoneyMath.EqualsMoney(sum, amount))
                    throw new ConflictException("Tổng phân bổ phải bằng chiết khấu.");
            }
            else if (lines.Count > 0)
            {
                throw new ConflictException("Không gửi dòng phân bổ khi không bật phân bổ.");
            }

            var supplier = await _suppliers.GetTrackedAsync(supplierId, cancellationToken)
                ?? throw new NotFoundException("Nhà cung cấp không tồn tại.");

            if (amount > supplier.CurrentDebt)
                throw new ConflictException("Chiết khấu vượt nợ hiện tại.");

            if (request.AllocateToDocuments)
            {
                foreach (var line in lines)
                {
                    var gr = await _goodsReceipts.GetTrackedAsync(line.GoodsReceiptId, cancellationToken)
                        ?? throw new NotFoundException("Phiếu nhập không tồn tại.");
                    if (gr.SupplierId != supplierId)
                        throw new ConflictException($"Phiếu {gr.Code} không thuộc nhà cung cấp này.");
                    var d = MoneyMath.Round(line.Amount);
                    if (d <= 0m)
                        throw new ConflictException("Chiết khấu phân bổ phải dương.");
                    if (d > gr.SupplierDebtDelta)
                        throw new ConflictException($"Chiết khấu vượt nợ còn lại của phiếu {gr.Code}.");
                }

                foreach (var line in lines)
                {
                    var gr = (await _goodsReceipts.GetTrackedAsync(line.GoodsReceiptId, cancellationToken))!;
                    var d = MoneyMath.Round(line.Amount);
                    gr.SupplierPayableDiscountPortion = MoneyMath.Round(gr.SupplierPayableDiscountPortion + d);
                    gr.SupplierDebtDelta =
                        MoneyMath.Round(Math.Max(0m, gr.Total - gr.PaidAmount - gr.SupplierPayableDiscountPortion));
                }
            }

            supplier.CurrentDebt = MoneyMath.Round(Math.Max(0m, supplier.CurrentDebt - amount));

            var discount = new SupplierPayableDiscount
            {
                Id = Guid.NewGuid(),
                Code = await _payables.GenerateNextDiscountCodeAsync(cancellationToken),
                SupplierId = supplierId,
                OccurredAtUtc = request.OccurredAtUtc ?? DateTime.UtcNow,
                PerformerUserId = request.PerformerUserId,
                TotalAmount = amount,
                Note = request.Note?.Trim(),
                AllocateToDocuments = request.AllocateToDocuments,
            };

            foreach (var line in lines)
            {
                discount.Lines.Add(new SupplierPayableDiscountLine
                {
                    Id = Guid.NewGuid(),
                    DiscountId = discount.Id,
                    GoodsReceiptId = line.GoodsReceiptId,
                    Amount = MoneyMath.Round(line.Amount),
                });
            }

            await _payables.AddDiscountAsync(discount, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Supplier {SupplierId} discount {Code} amount {Amount}", supplierId, discount.Code,
                amount);

            return new SupplierPayableDiscountDto(discount.Id, discount.Code, discount.OccurredAtUtc,
                discount.TotalAmount);
        }, cancellationToken);
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new ForbiddenException("Authentication required.");
    }
}
