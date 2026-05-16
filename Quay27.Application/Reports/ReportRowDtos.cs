namespace Quay27.Application.Reports;

public sealed class EndOfDayReportRowApiDto
{
    public string Code { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public string? CustomerName { get; init; }
    public string? SellerDisplayName { get; init; }
    public int Quantity { get; init; }
    public decimal SubtotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal OtherRevenue { get; init; }
    public decimal Vat { get; init; }
    public decimal Rounding { get; init; }
    public decimal ReturnFee { get; init; }
    public decimal PaidAmount { get; init; }
}

/// <summary>Unified grid row for all end-of-day concerns (only relevant fields are set).</summary>
public sealed class EndOfDayUnifiedRowApiDto
{
    public string? Label { get; init; }
    public string? Code { get; init; }
    public DateTime? OccurredAtUtc { get; init; }
    public string? CustomerName { get; init; }
    public string? SellerDisplayName { get; init; }
    public string? CategoryName { get; init; }
    public string? CounterpartyName { get; init; }
    public string? EntryType { get; init; }
    public string? ProductCode { get; init; }
    public string? ProductName { get; init; }
    public int? Quantity { get; init; }
    public int? ReturnQuantity { get; init; }
    public decimal? SubtotalAmount { get; init; }
    public decimal? DiscountAmount { get; init; }
    public decimal? PaidAmount { get; init; }
    public decimal? Amount { get; init; }
    public decimal? CashAmount { get; init; }
    public decimal? TransferAmount { get; init; }
    public decimal? CardAmount { get; init; }
    public decimal? ReturnValue { get; init; }
    public decimal? NetRevenue { get; init; }
    public decimal? ListPrice { get; init; }
    public decimal? Variance { get; init; }
    public int? TransactionCount { get; init; }
    public string? SourceCode { get; init; }
    public decimal? Value { get; init; }
}

public sealed class SalesReportRowApiDto
{
    public string Code { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public string? CustomerName { get; init; }
    public decimal SubtotalAmount { get; init; }
    public decimal DiscountAmount { get; init; }
    public decimal PaidAmount { get; init; }
}

public sealed class OrdersReportRowApiDto
{
    public string Code { get; init; } = string.Empty;
    public DateTime CreatedAtUtc { get; init; }
    public string? CustomerName { get; init; }
    public string Status { get; init; } = string.Empty;
    public decimal AmountDue { get; init; }
    public decimal AmountPaid { get; init; }
}

public sealed class ProductsReportRowApiDto
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? GroupName { get; init; }
    public decimal SalePrice { get; init; }
    public int Stock { get; init; }
    public string? RowStatus { get; init; }
}

public sealed class CustomersReportRowApiDto
{
    public string CustomerCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string Phone1 { get; init; } = string.Empty;
    public string CustomerGroup { get; init; } = string.Empty;
    public decimal CurrentDebt { get; init; }
    public decimal TotalSalesNet { get; init; }
}

public sealed class SuppliersReportRowApiDto
{
    public string Code { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string? SupplierGroupName { get; init; }
    public decimal CurrentDebt { get; init; }
    public decimal TotalPurchase { get; init; }
}
