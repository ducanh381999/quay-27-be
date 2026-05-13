using Quay27.Application.Common.Exceptions;
using Quay27.Application.Repositories;
using Quay27.Domain.Entities;

namespace Quay27.Application.Services;

/// <summary>Điều chỉnh tồn kho khi lưu phiếu nhập / trả hàng nhập (full reconcile).</summary>
public static class PurchasingReceiptStockHelper
{
    public static int LineQuantityToInt(decimal quantity)
    {
        if (quantity < 0)
            throw new InvalidOperationException("Số lượng không được âm.");
        if (quantity != Math.Truncate(quantity))
            throw new InvalidOperationException("Số lượng phải là số nguyên.");
        if (quantity > int.MaxValue)
            throw new InvalidOperationException("Số lượng vượt giới hạn.");
        return (int)quantity;
    }

    private static bool IsReceived(string status) =>
        string.Equals(status.Trim(), "received", StringComparison.OrdinalIgnoreCase);

    private static bool IsReturned(string status) =>
        string.Equals(status.Trim(), "returned", StringComparison.OrdinalIgnoreCase);

    public static async Task ReconcileGoodsReceiptStockAsync(
        IProductRepository products,
        string previousStatus,
        GoodsReceipt entity,
        IReadOnlyList<(Guid ProductId, decimal Quantity)> previousLines,
        CancellationToken cancellationToken)
    {
        var prev = previousStatus.Trim();
        var now = (entity.Status ?? string.Empty).Trim();

        if (IsReceived(prev))
        {
            foreach (var (productId, qty) in previousLines)
            {
                await AdjustStockAsync(products, productId, -LineQuantityToInt(qty), cancellationToken);
            }
        }

        if (IsReceived(now))
        {
            foreach (var line in entity.Lines)
            {
                await AdjustStockAsync(products, line.ProductId, LineQuantityToInt(line.Quantity), cancellationToken);
            }
        }
    }

    public static async Task ReconcileReturnReceiptStockAsync(
        IProductRepository products,
        string previousStatus,
        ReturnReceipt entity,
        IReadOnlyList<(Guid ProductId, decimal Quantity)> previousLines,
        CancellationToken cancellationToken)
    {
        var prev = previousStatus.Trim();
        var now = (entity.Status ?? string.Empty).Trim();

        if (IsReturned(prev))
        {
            foreach (var (productId, qty) in previousLines)
            {
                await AdjustStockAsync(products, productId, LineQuantityToInt(qty), cancellationToken);
            }
        }

        if (IsReturned(now))
        {
            foreach (var line in entity.Lines)
            {
                var q = LineQuantityToInt(line.Quantity);
                var p = await products.GetTrackedByIdAsync(line.ProductId, cancellationToken)
                        ?? throw new NotFoundException("Hàng hóa không tồn tại.");
                if (p.Stock < q)
                {
                    throw new InvalidOperationException(
                        $"Tồn kho không đủ để trả hàng ({p.Code}: tồn {p.Stock}, cần {q}).");
                }
            }

            foreach (var line in entity.Lines)
            {
                await AdjustStockAsync(products, line.ProductId, -LineQuantityToInt(line.Quantity), cancellationToken);
            }
        }
    }

    private static async Task AdjustStockAsync(
        IProductRepository products,
        Guid productId,
        int delta,
        CancellationToken cancellationToken)
    {
        if (delta == 0)
            return;

        var p = await products.GetTrackedByIdAsync(productId, cancellationToken)
                ?? throw new NotFoundException("Hàng hóa không tồn tại.");

        try
        {
            checked
            {
                p.Stock += delta;
            }
        }
        catch (OverflowException)
        {
            throw new InvalidOperationException($"Tồn kho vượt giới hạn cho hàng {p.Code}.");
        }

        if (p.Stock < 0)
        {
            throw new InvalidOperationException($"Tồn kho âm sau điều chỉnh ({p.Code}).");
        }
    }
}
