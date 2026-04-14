namespace Quay27.Domain.Entities;

public class Customer
{
    public Guid Id { get; set; }

    /// <summary>Sheet column stt — display order within a day.</summary>
    public int SortOrder { get; set; }

    /// <summary>Mã HĐ</summary>
    public string InvoiceCode { get; set; } = string.Empty;

    /// <summary>TG lên bill</summary>
    public DateTime BillCreatedAt { get; set; }

    /// <summary>Tên khách + địa chỉ (combined).</summary>
    public string NameAddress { get; set; } = string.Empty;

    public string CreateMachine { get; set; } = string.Empty;
    public string DraftStaff { get; set; } = string.Empty;
    public string Quantity { get; set; } = string.Empty;
    public string TotalAmount { get; set; } = string.Empty;


    /// <summary>NV Lắp CM</summary>
    public string InstallStaffCm { get; set; } = string.Empty;

    public bool ManagerApproved { get; set; }
    public bool Kio27Received { get; set; }
    public bool Export27 { get; set; }

    /// <summary>Full sheet — tự xuất; khi true thì job cuối ngày không tăng SheetDate.</summary>
    public bool FullSelfExport { get; set; }

    /// <summary>Ghi chú</summary>
    public string Notes { get; set; } = string.Empty;

    /// <summary>Ghi chú ai gửi hàng</summary>
    public string GoodsSenderNote { get; set; } = string.Empty;

    /// <summary>Ghi thêm gì thì tùy</summary>
    public string AdditionalNotes { get; set; } = string.Empty;

    public DateOnly SheetDate { get; set; }
    public string Status { get; set; } = string.Empty;
    public bool IsDuplicate { get; set; }
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedDate { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }

    public ICollection<CustomerQueue> CustomerQueues { get; set; } = new List<CustomerQueue>();
    public ICollection<DuplicateFlag> DuplicateFlags { get; set; } = new List<DuplicateFlag>();
    public ICollection<CustomerVersion> CustomerVersions { get; set; } = new List<CustomerVersion>();
}
