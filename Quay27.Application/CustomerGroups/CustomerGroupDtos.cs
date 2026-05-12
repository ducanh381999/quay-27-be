namespace Quay27.Application.CustomerGroups;

public sealed class CustomerGroupDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal? DiscountAmount { get; init; }
    public bool DiscountIsPercent { get; init; }
    public IReadOnlyList<CustomerGroupConditionDto> Conditions { get; init; } = Array.Empty<CustomerGroupConditionDto>();
    public bool CombineAllConditions { get; init; }
    public string MembershipUpdateMode { get; init; } = "none";
    public bool IsAutoMembershipSync { get; init; }
    public DateTime CreatedDate { get; init; }
    public DateTime? UpdatedDate { get; init; }
}

public sealed class CustomerGroupTreeDto
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public IReadOnlyList<CustomerGroupTreeDto> Children { get; init; } = Array.Empty<CustomerGroupTreeDto>();
}

public sealed class CreateCustomerGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? DiscountAmount { get; set; }
    public bool DiscountIsPercent { get; set; }
    public IReadOnlyList<CustomerGroupConditionDto>? Conditions { get; set; }
    public bool CombineAllConditions { get; set; } = true;
    /// <summary>add | replace | none</summary>
    public string MembershipUpdateMode { get; set; } = "none";
    public bool IsAutoMembershipSync { get; set; }
}

public sealed class UpdateCustomerGroupRequest
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public decimal? DiscountAmount { get; set; }
    public bool DiscountIsPercent { get; set; }
    public IReadOnlyList<CustomerGroupConditionDto>? Conditions { get; set; }
    public bool CombineAllConditions { get; set; } = true;
    public string MembershipUpdateMode { get; set; } = "none";
    public bool IsAutoMembershipSync { get; set; }
}
