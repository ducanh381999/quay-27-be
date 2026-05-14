using Microsoft.Extensions.Logging;
using Quay27.Application.Abstractions;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.Purchasing;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

public class GoodsReceiptService : IGoodsReceiptService
{
    private readonly IGoodsReceiptRepository _receipts;
    private readonly IProductRepository _products;
    private readonly ISupplierRepository _suppliers;
    private readonly IReceivingAccountRepository _receivingAccounts;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICashbookSyncService _cashbookSync;
    private readonly ILogger<GoodsReceiptService> _logger;

    public GoodsReceiptService(
        IGoodsReceiptRepository receipts,
        IProductRepository products,
        ISupplierRepository suppliers,
        IReceivingAccountRepository receivingAccounts,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        ICashbookSyncService cashbookSync,
        ILogger<GoodsReceiptService> logger)
    {
        _receipts = receipts;
        _products = products;
        _suppliers = suppliers;
        _receivingAccounts = receivingAccounts;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _cashbookSync = cashbookSync;
        _logger = logger;
    }

    public async Task<ReceiptImportPreviewResult> PreviewImportAsync(byte[] fileBytes, string fileName, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        if (fileBytes.Length == 0)
            throw new InvalidOperationException("File import rỗng.");
        if (!string.Equals(Path.GetExtension(fileName ?? string.Empty), ".xlsx", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ hỗ trợ file .xlsx.");

        var parsed = ReceiptExcelReader.ReadRows(fileBytes);
        var items = new List<ReceiptImportPreviewItem>();
        var errors = new List<string>();

        foreach (var row in parsed)
        {
            Product? product = null;
            if (Guid.TryParse(row.ProductCode, out var productId))
            {
                product = await _products.GetByIdAsync(productId, cancellationToken);
            }

            if (product is null)
            {
                var list = await _products.ListAsync(new Products.ProductQuery
                {
                    Search = row.ProductCode,
                    Status = "all",
                    Stock = "all",
                    DirectSale = "all",
                }, cancellationToken);
                product = list.Items.FirstOrDefault(x =>
                    string.Equals(x.Code, row.ProductCode, StringComparison.OrdinalIgnoreCase));
            }

            if (product is null)
            {
                errors.Add($"Dòng {row.RowNumber}: Mã hàng không có trên hệ thống hoặc ngừng kinh doanh");
                continue;
            }

            var lineTotal = Math.Max(0m, row.Quantity * row.UnitPrice - row.Discount);
            items.Add(new ReceiptImportPreviewItem(
                row.RowNumber,
                product.Id,
                product.Code,
                product.Name,
                product.Group?.Name ?? string.Empty,
                row.Quantity,
                row.UnitPrice,
                row.Discount,
                lineTotal));
        }

        return new ReceiptImportPreviewResult(parsed.Count, items.Count, errors.Count, errors, items);
    }

    public Task<IReadOnlyList<GoodsReceiptListItemDto>> ListAsync(ReceiptListQuery query, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        return _receipts.ListAsync(query, cancellationToken);
    }

    public async Task<GoodsReceiptDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var entity = await _receipts.GetProjectedAsync(id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<GoodsReceiptSupplierPaymentCashbookRowDto>?> ListSupplierPaymentCashbookEntriesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        if (!await _receipts.ExistsAsync(id, cancellationToken))
            return null;
        return await _receipts.ListSupplierPaymentCashbookRowsAsync(id, cancellationToken);
    }

    public Task<GoodsReceiptDto> CreateAsync(CreateGoodsReceiptRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = new GoodsReceipt
            {
                Id = Guid.NewGuid(),
                Code = await _receipts.GenerateNextCodeAsync(cancellationToken),
                CreatedBy = _currentUser.Username,
                CreatedDate = DateTime.UtcNow,
            };
            await ApplyRequestAsync(entity, request, cancellationToken);
            await _receipts.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Goods receipt {Code} created by {User}", entity.Code, _currentUser.Username);
            return (await GetByIdAsync(entity.Id, cancellationToken))!;
        }, cancellationToken);
    }

    public Task<GoodsReceiptDto> UpdateAsync(Guid id, CreateGoodsReceiptRequest request, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await _receipts.GetTrackedAsync(id, cancellationToken)
                ?? throw new NotFoundException("Phiếu nhập không tồn tại.");
            await ApplyRequestAsync(entity, request, cancellationToken);
            entity.UpdatedBy = _currentUser.Username;
            entity.UpdatedDate = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(entity.Id, cancellationToken))!;
        }, cancellationToken);
    }

    private async Task ApplyRequestAsync(
        GoodsReceipt entity,
        CreateGoodsReceiptRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Lines.Count == 0)
            throw new InvalidOperationException("Phiếu nhập phải có ít nhất 1 hàng hóa.");

        var previousStatus = entity.Status?.Trim() ?? string.Empty;
        var previousLineQuantities = entity.Lines.Select(l => (l.ProductId, l.Quantity)).ToList();
        var previousPaymentAllocationIds = entity.PaymentAllocations.Select(a => a.Id).ToList();

        Supplier? oldSupplier = null;
        if (entity.SupplierId.HasValue)
        {
            oldSupplier = await _suppliers.GetTrackedAsync(entity.SupplierId.Value, cancellationToken);
        }

        Supplier? newSupplier = null;
        if (request.SupplierId.HasValue)
        {
            newSupplier = await _suppliers.GetTrackedAsync(request.SupplierId.Value, cancellationToken)
                ?? throw new NotFoundException("Nhà cung cấp không tồn tại.");
        }

        var receiptDate = request.ReceiptDate ?? DateTime.Now;
        var normalizedLines = new List<GoodsReceiptLine>();
        decimal subtotal = 0m;
        foreach (var line in request.Lines)
        {
            var product = await _products.GetByIdAsync(line.ProductId, cancellationToken)
                ?? throw new NotFoundException("Hàng hóa không tồn tại.");
            if (product.RowStatus != "active")
                throw new InvalidOperationException($"Hàng hóa {product.Code} đã ngừng kinh doanh.");

            var quantity = Math.Max(0m, line.Quantity);
            var unitPrice = Math.Max(0m, line.UnitPrice);
            var discount = Math.Max(0m, line.Discount);
            var lineTotal = Math.Max(0m, quantity * unitPrice - discount);
            subtotal += lineTotal;

            var unitSnapshot = !string.IsNullOrWhiteSpace(line.Unit)
                ? line.Unit.Trim()
                : (product.Group?.Name ?? string.Empty);

            normalizedLines.Add(new GoodsReceiptLine
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ProductCodeSnapshot = product.Code,
                ProductNameSnapshot = product.Name,
                UnitSnapshot = unitSnapshot,
                Quantity = quantity,
                UnitPrice = unitPrice,
                Discount = discount,
                LineTotal = lineTotal,
                Note = NormalizeLineNote(line.Note),
            });
        }

        var discountTotal = Math.Max(0m, request.Discount);
        var total = Math.Max(0m, subtotal - discountTotal);
        var paidAmount = Math.Max(0m, request.PaidAmount);
        var supplierDebtDelta = Math.Max(0m, total - paidAmount - entity.SupplierPayableDiscountPortion);
        var oldDebtDelta = entity.SupplierDebtDelta;
        var oldSubtotal = entity.Subtotal;

        entity.SupplierId = request.SupplierId;
        entity.Supplier = newSupplier;
        entity.Status = string.IsNullOrWhiteSpace(request.Status) ? "received" : request.Status.Trim();
        entity.ReceiptDate = receiptDate;
        entity.Subtotal = subtotal;
        entity.Discount = discountTotal;
        entity.Total = total;
        entity.PaidAmount = paidAmount;
        entity.SupplierDebtDelta = supplierDebtDelta;
        entity.Notes = request.Notes?.Trim() ?? string.Empty;

        entity.Lines.Clear();
        foreach (var line in normalizedLines)
        {
            entity.Lines.Add(line);
        }

        entity.PaymentAllocations.Clear();
        if (request.PaymentAllocations is not null)
        {
            foreach (var allocation in request.PaymentAllocations)
            {
                if ((allocation.PaymentMethod == "card" || allocation.PaymentMethod == "bankTransfer") &&
                    allocation.ReceivingAccountId.HasValue)
                {
                    var account = await _receivingAccounts.GetByIdAsync(allocation.ReceivingAccountId.Value, cancellationToken);
                    if (account is null)
                        throw new InvalidOperationException("Tài khoản nhận không tồn tại.");
                }

                entity.PaymentAllocations.Add(new SupplierPaymentAllocation
                {
                    Id = Guid.NewGuid(),
                    GoodsReceiptId = entity.Id,
                    ReturnReceiptId = null,
                    PaymentMethod = allocation.PaymentMethod,
                    Amount = Math.Max(0m, allocation.Amount),
                    ReceivingAccountId = allocation.ReceivingAccountId,
                });
            }
        }

        // Debt impacts: supplier is optional.
        if (oldSupplier is not null)
        {
            oldSupplier.CurrentDebt = Math.Max(0m, oldSupplier.CurrentDebt - oldDebtDelta);
            oldSupplier.TotalPurchase = Math.Max(0m, oldSupplier.TotalPurchase - oldSubtotal);
        }
        if (newSupplier is not null)
        {
            newSupplier.CurrentDebt += supplierDebtDelta;
            newSupplier.TotalPurchase += subtotal;
        }

        await PurchasingReceiptStockHelper.ReconcileGoodsReceiptStockAsync(
            _products, previousStatus, entity, previousLineQuantities, cancellationToken);
        await _cashbookSync.SyncGoodsReceiptSupplierPaymentsAsync(entity, previousPaymentAllocationIds, cancellationToken);
    }

    private static GoodsReceiptDto Map(GoodsReceipt entity)
    {
        return new GoodsReceiptDto(
            entity.Id,
            entity.Code,
            entity.SupplierId,
            entity.Supplier?.Code,
            entity.Supplier?.Name,
            entity.Status,
            entity.ReceiptDate,
            entity.Subtotal,
            entity.Discount,
            entity.Total,
            entity.PaidAmount,
            entity.SupplierDebtDelta,
            entity.Notes,
            entity.CreatedBy,
            entity.Lines.Select(x => new ReceiptLineDto(
                x.Id,
                x.ProductId,
                x.ProductCodeSnapshot,
                x.ProductNameSnapshot,
                x.UnitSnapshot,
                x.Quantity,
                x.UnitPrice,
                x.Discount,
                x.LineTotal,
                x.Note)).ToList(),
            entity.PaymentAllocations.Select(x => new PaymentAllocationDto(
                x.Id,
                x.PaymentMethod,
                x.Amount,
                x.ReceivingAccountId,
                x.ReceivingAccount?.Name)).ToList());
    }

    private static string? NormalizeLineNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note))
        {
            return null;
        }

        var t = note.Trim();
        return t.Length <= 500 ? t : t[..500];
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new ForbiddenException("Authentication required.");
    }
}
