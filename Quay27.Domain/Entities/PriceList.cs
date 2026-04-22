namespace Quay27.Domain.Entities;

public class PriceList
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Status { get; set; } = "active";
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public string FormulaSource { get; set; } = "costPrice";
    public string FormulaOperation { get; set; } = "add";
    public decimal FormulaValue { get; set; }
    public string FormulaUnit { get; set; } = "vnd";
    public bool RoundEnabled { get; set; }
    public int RoundTo { get; set; } = 1;
    public string SalesRuleMode { get; set; } = "allow";
    public string BranchIdsJson { get; set; } = "[]";
    public string CustomerGroupIdsJson { get; set; } = "[]";
    public string CashierIdsJson { get; set; } = "[]";
    public bool IsDeleted { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<PriceListItem> Items { get; set; } = new List<PriceListItem>();
}
