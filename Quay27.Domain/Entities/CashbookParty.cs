namespace Quay27.Domain.Entities;

/// <summary>Người nộp / người nhận loại Khác (tạo mới từ sổ quỹ).</summary>
public sealed class CashbookParty
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Province { get; set; }
    public string? Ward { get; set; }
    public string? Note { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }
}
