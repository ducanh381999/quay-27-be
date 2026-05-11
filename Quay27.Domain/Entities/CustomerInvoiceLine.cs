namespace Quay27.Domain.Entities;

/// <summary>
/// Chi tiết dòng hàng trên hóa đơn khách (sheet). Bảng đọc cho dashboard Top 10;
/// luồng nhập/cập nhật từ UI sheet là hạng mục riêng.
/// </summary>
public class CustomerInvoiceLine
{
    public Guid Id { get; set; }
    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid? ProductId { get; set; }
    public Product? Product { get; set; }

    public string ProductNameSnapshot { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal Amount { get; set; }
}
