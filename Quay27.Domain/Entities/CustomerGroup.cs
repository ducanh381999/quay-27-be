namespace Quay27.Domain.Entities;

public class CustomerGroup
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Discount value; when <see cref="DiscountIsPercent"/> is true, 0–100; else amount in VND.</summary>
    public decimal? DiscountAmount { get; set; }

    public bool DiscountIsPercent { get; set; }

    /// <summary>JSON array of rule rows: field, op, value.</summary>
    public string RulesJson { get; set; } = "[]";

    public bool CombineAllConditions { get; set; } = true;

    /// <summary>add | replace | none</summary>
    public string MembershipUpdateMode { get; set; } = "none";

    public bool IsAutoMembershipSync { get; set; }

    public bool IsDeleted { get; set; }
    public DateTime CreatedDate { get; set; }
    public DateTime? UpdatedDate { get; set; }
}
