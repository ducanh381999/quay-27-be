namespace Quay27.Domain.Entities;

/// <summary>Điều chỉnh nợ cần trả nhà cung cấp (delta có dấu).</summary>
public sealed class SupplierDebtAdjustment
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public Guid SupplierId { get; set; }
    public Supplier? Supplier { get; set; }
    public DateTime OccurredAtUtc { get; set; }
    /// <summary>Thay đổi nợ: dương = tăng nợ, âm = giảm nợ.</summary>
    public decimal Delta { get; set; }
    public string Description { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
