namespace Quay27.Application.Suppliers;

public record CreateSupplierRequest(
    string Name,
    string? Code,
    string? Phone,
    string? Email,
    string? Address,
    string? Region,
    string? Ward,
    Guid? SupplierGroupId,
    string? Notes,
    string? CompanyName,
    string? TaxCode,
    decimal? InitialDebt,
    decimal? CurrentDebt);
