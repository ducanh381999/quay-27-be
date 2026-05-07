namespace Quay27.Application.Suppliers;

public record SupplierGroupDto(
    Guid Id,
    string Name,
    string? Notes,
    int SupplierCount,
    DateTime CreatedDate,
    string CreatedBy);
