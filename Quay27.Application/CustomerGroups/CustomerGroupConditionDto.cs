namespace Quay27.Application.CustomerGroups;

/// <summary>One row in the advanced rules builder (stored in <see cref="CustomerGroup.RulesJson"/> as JSON array).</summary>
public sealed class CustomerGroupConditionDto
{
    /// <summary>TotalRevenue, TotalInvoiced, RewardPoint, …</summary>
    public string Field { get; set; } = string.Empty;

    /// <summary>Operators: &gt;, &lt;, &gt;=, &lt;=, =</summary>
    public string Op { get; set; } = string.Empty;

    /// <summary>Serialized threshold (number, ISO date yyyy-MM-dd, month 1–12, Nam/Nữ, individual/company, location substring).</summary>
    public string? Value { get; set; }
}
