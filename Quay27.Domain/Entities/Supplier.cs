namespace Quay27.Domain.Entities;

public class Supplier
{
    public Guid Id { get; set; }

    /// <summary>Mã nhà cung cấp, dạng NCC000001 — sinh tự động khi tạo nếu rỗng.</summary>
    public string Code { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    /// <summary>Khu vực — free text (Tỉnh/Thành phố).</summary>
    public string Region { get; set; } = string.Empty;

    /// <summary>Phường/Xã — free text.</summary>
    public string Ward { get; set; } = string.Empty;

    public Guid? SupplierGroupId { get; set; }
    public SupplierGroup? SupplierGroup { get; set; }

    public string Notes { get; set; } = string.Empty;
    public string CompanyName { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;

    /// <summary>Nợ ban đầu khi import "Cập nhật dư nợ cuối".</summary>
    public decimal InitialDebt { get; set; }

    /// <summary>Tổng tiền mua tích luỹ (v1: cập nhật trực tiếp).</summary>
    public decimal TotalPurchase { get; set; }

    /// <summary>Tổng tiền trả hàng tích luỹ.</summary>
    public decimal TotalReturn { get; set; }

    /// <summary>Nợ cần trả hiện tại (snapshot v1; sẽ tính lại khi có module Nhập/Trả hàng).</summary>
    public decimal CurrentDebt { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedDate { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}
