namespace Quay27.Application.Suppliers;

/// <summary>Partial update — chỉ field khác null mới được áp dụng.</summary>
public record UpdateSupplierRequest(
    string? Name,
    string? Phone,
    string? Email,
    string? Address,
    string? Region,
    string? Ward,
    Guid? SupplierGroupId,
    bool? ClearSupplierGroup,
    string? Notes,
    string? CompanyName,
    string? TaxCode,
    decimal? InitialDebt,
    decimal? CurrentDebt,
    bool? IsActive);
