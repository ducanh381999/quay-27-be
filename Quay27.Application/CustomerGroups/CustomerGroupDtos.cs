namespace Quay27.Application.CustomerGroups;

public sealed class CustomerGroupDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public DateTime CreatedDate { get; init; }
    public DateTime? UpdatedDate { get; init; }
}

public sealed class CreateCustomerGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}

public sealed class UpdateCustomerGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
}
