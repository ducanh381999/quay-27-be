namespace Quay27.Domain.Entities;

/// <summary>Danh mục loại thu / loại chi (sổ quỹ).</summary>
public sealed class PaymentCategory
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;

    /// <summary>Receipt | Expense</summary>
    public string Kind { get; set; } = "Expense";
}
