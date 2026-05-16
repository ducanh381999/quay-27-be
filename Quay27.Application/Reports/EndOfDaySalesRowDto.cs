namespace Quay27.Application.Reports;

public sealed class EndOfDaySalesRowDto
{
    public string Code { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public string? CustomerName { get; init; }
    public string? SellerDisplayName { get; init; }
    public int Quantity { get; init; }
    public decimal SubtotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal PaidAmount { get; init; }
}
