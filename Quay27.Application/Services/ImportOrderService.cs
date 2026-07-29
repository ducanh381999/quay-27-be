using Microsoft.Extensions.Logging;
using Quay27.Application.Abstractions;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.Purchasing;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

public class ImportOrderService : IImportOrderService
{
    private readonly IImportOrderRepository _orders;
    private readonly IProductRepository _products;
    private readonly ISupplierRepository _suppliers;
    private readonly ICurrentUser _currentUser;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<ImportOrderService> _logger;

    public ImportOrderService(
        IImportOrderRepository orders,
        IProductRepository products,
        ISupplierRepository suppliers,
        ICurrentUser currentUser,
        IUnitOfWork unitOfWork,
        ILogger<ImportOrderService> logger)
    {
        _orders = orders;
        _products = products;
        _suppliers = suppliers;
        _currentUser = currentUser;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task<IReadOnlyList<ImportOrderListItemDto>> ListAsync(
        ImportOrderListQuery query,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        return _orders.ListAsync(query, cancellationToken);
    }

    public async Task<ImportOrderDetailDto?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        var entity = await _orders.GetProjectedAsync(id, cancellationToken);
        return entity is null ? null : Map(entity);
    }

    public async Task<IReadOnlyList<GoodsReceiptSupplierPaymentCashbookRowDto>?> ListSupplierPaymentCashbookEntriesAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        if (!await _orders.ExistsAsync(id, cancellationToken))
            return null;
        return Array.Empty<GoodsReceiptSupplierPaymentCashbookRowDto>();
    }

    public Task<ImportOrderDetailDto> CreateAsync(
        CreateImportOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = new ImportOrder
            {
                Id = Guid.NewGuid(),
                Code = await _orders.GenerateNextCodeAsync(cancellationToken),
                CreatedBy = _currentUser.Username,
                OrderedBy = _currentUser.Username,
                CreatedDate = DateTime.UtcNow,
            };
            await ApplyRequestAsync(entity, request, cancellationToken);
            await _orders.AddAsync(entity, cancellationToken);
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            _logger.LogInformation("Import order {Code} created by {User}", entity.Code, _currentUser.Username);
            return (await GetByIdAsync(entity.Id, cancellationToken))!;
        }, cancellationToken);
    }

    public Task<ImportOrderDetailDto> UpdateAsync(
        Guid id,
        CreateImportOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        return _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var entity = await _orders.GetTrackedAsync(id, cancellationToken)
                ?? throw new NotFoundException("Phiếu đặt hàng nhập không tồn tại.");
            await ApplyRequestAsync(entity, request, cancellationToken);
            entity.UpdatedBy = _currentUser.Username;
            entity.UpdatedDate = DateTime.UtcNow;
            await _unitOfWork.SaveChangesAsync(cancellationToken);
            return (await GetByIdAsync(entity.Id, cancellationToken))!;
        }, cancellationToken);
    }

    public async Task<ImportOrderSuggestResult> SuggestAsync(
        ImportOrderSuggestRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureAuthenticated();
        if (!request.GroupId.HasValue)
            return new ImportOrderSuggestResult([]);

        var products = await _products.ListByGroupIdsAsync([request.GroupId.Value], cancellationToken);
        var stockMode = (request.StockMode ?? "ignore_stock").Trim().ToLowerInvariant();
        var qtyMode = (request.QtyMode ?? "none").Trim().ToLowerInvariant();

        IEnumerable<Product> filtered = products.Where(p => p.RowStatus == "active");
        filtered = stockMode switch
        {
            "below_min" => filtered.Where(p => p.MinStock.HasValue && p.Stock < p.MinStock.Value),
            "out_of_stock" => filtered.Where(p => p.Stock <= 0),
            _ => filtered,
        };

        var lines = new List<ImportOrderSuggestLineDto>();
        foreach (var product in filtered)
        {
            var qty = await ResolveSuggestedQtyAsync(product, qtyMode, cancellationToken);
            if (qty <= 0) continue;
            lines.Add(new ImportOrderSuggestLineDto(
                product.Id,
                product.Code,
                product.Name,
                "Cái",
                qty,
                product.CostPrice));
        }

        return new ImportOrderSuggestResult(lines);
    }

    private async Task<decimal> ResolveSuggestedQtyAsync(
        Product product,
        string qtyMode,
        CancellationToken cancellationToken)
    {
        switch (qtyMode)
        {
            case "min_minus_current":
                if (!product.MinStock.HasValue) return 0m;
                return Math.Max(0m, product.MinStock.Value - product.Stock);
            case "max_minus_current":
                if (!product.MaxStock.HasValue) return 0m;
                return Math.Max(0m, product.MaxStock.Value - product.Stock);
            case "last_order":
                return await _orders.GetLastOrderedQuantityAsync(product.Id, cancellationToken) ?? 1m;
            case "sold_in_period":
                // Sales aggregation not wired yet — default qty 1.
                return 1m;
            case "none":
            default:
                return 1m;
        }
    }

    private async Task ApplyRequestAsync(
        ImportOrder entity,
        CreateImportOrderRequest request,
        CancellationToken cancellationToken)
    {
        if (request.Lines.Count == 0)
            throw new InvalidOperationException("Phiếu đặt hàng nhập phải có ít nhất 1 hàng hóa.");

        if (request.SupplierId.HasValue)
        {
            _ = await _suppliers.GetTrackedAsync(request.SupplierId.Value, cancellationToken)
                ?? throw new NotFoundException("Nhà cung cấp không tồn tại.");
        }

        var previousReceived = entity.Lines
            .GroupBy(l => l.ProductId)
            .ToDictionary(g => g.Key, g => g.Sum(x => x.QuantityReceived));

        var normalizedLines = new List<ImportOrderLine>();
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
                : "Cái";

            previousReceived.TryGetValue(product.Id, out var qtyReceived);

            normalizedLines.Add(new ImportOrderLine
            {
                Id = Guid.NewGuid(),
                ProductId = product.Id,
                ProductCodeSnapshot = product.Code,
                ProductNameSnapshot = product.Name,
                UnitSnapshot = unitSnapshot,
                Quantity = quantity,
                QuantityReceived = Math.Min(qtyReceived, quantity),
                UnitPrice = unitPrice,
                Discount = discount,
                LineTotal = lineTotal,
                Note = NormalizeLineNote(line.Note),
            });
        }

        var discountTotal = Math.Max(0m, request.Discount);
        var total = Math.Max(0m, subtotal - discountTotal);

        entity.SupplierId = request.SupplierId;
        entity.Status = string.IsNullOrWhiteSpace(request.Status) ? "draft" : request.Status.Trim();
        entity.ExpectedReceiptDate = request.ExpectedReceiptDate;
        entity.Subtotal = subtotal;
        entity.Discount = discountTotal;
        entity.Total = total;
        entity.PaidAmount = Math.Max(0m, request.PaidAmount);
        entity.Notes = request.Notes?.Trim() ?? string.Empty;

        entity.Lines.Clear();
        foreach (var line in normalizedLines)
            entity.Lines.Add(line);
    }

    private static ImportOrderDetailDto Map(ImportOrder entity)
    {
        return new ImportOrderDetailDto(
            entity.Id,
            entity.Code,
            entity.Status,
            entity.SupplierId,
            entity.Supplier?.Code,
            entity.Supplier?.Name,
            entity.CreatedDate,
            entity.CreatedBy,
            entity.OrderedBy ?? entity.CreatedBy,
            entity.ExpectedReceiptDate,
            string.IsNullOrWhiteSpace(entity.Notes) ? null : entity.Notes,
            entity.Subtotal,
            entity.Discount,
            entity.Total,
            entity.PaidAmount,
            "LK3",
            entity.Lines.Select(x => new ImportOrderLineDto(
                x.Id,
                x.ProductId,
                x.ProductCodeSnapshot,
                x.ProductNameSnapshot,
                x.UnitSnapshot,
                x.Quantity,
                x.QuantityReceived,
                x.UnitPrice,
                x.Discount,
                x.LineTotal,
                x.Note)).ToList());
    }

    private static string? NormalizeLineNote(string? note)
    {
        if (string.IsNullOrWhiteSpace(note)) return null;
        var t = note.Trim();
        return t.Length <= 500 ? t : t[..500];
    }

    private void EnsureAuthenticated()
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new ForbiddenException("Authentication required.");
    }
}
