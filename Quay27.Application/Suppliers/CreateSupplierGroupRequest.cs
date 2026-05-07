namespace Quay27.Application.Suppliers;

public record CreateSupplierGroupRequest(string Name, string? Notes);

public record UpdateSupplierGroupRequest(string? Name, string? Notes);
