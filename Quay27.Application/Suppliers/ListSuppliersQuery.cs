namespace Quay27.Application.Suppliers;

/// <summary>Status filter: <c>all</c> = không lọc, <c>active</c> = đang hoạt động, <c>inactive</c> = ngừng hoạt động.</summary>
public record ListSuppliersQuery(
    string? Search = null,
    Guid? GroupId = null,
    string? Status = null,
    decimal? TotalPurchaseMin = null,
    decimal? TotalPurchaseMax = null,
    decimal? CurrentDebtMin = null,
    decimal? CurrentDebtMax = null,
    DateOnly? CreatedFrom = null,
    DateOnly? CreatedTo = null);
