namespace Quay27.Application.Reports;

public sealed class EndOfDayProductRowDto
{
    public string ProductCode { get; init; } = string.Empty;
    public string ProductName { get; init; } = string.Empty;
    public int SoldQuantity { get; init; }
    public decimal Revenue { get; init; }
    public int ReturnQuantity { get; init; }
    public decimal ReturnValue { get; init; }
    public decimal NetRevenue { get; init; }
    public decimal ListPrice { get; init; }
    public decimal Variance { get; init; }
}
