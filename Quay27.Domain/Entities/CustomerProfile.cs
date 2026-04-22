namespace Quay27.Domain.Entities;

public class CustomerProfile
{
    public Guid Id { get; set; }
    public string CustomerCode { get; set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string Phone1 { get; set; } = string.Empty;
    public string Phone2 { get; set; } = string.Empty;
    public DateOnly? Birthday { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Facebook { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;
    public string ProvinceCity { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;

    public string CustomerGroup { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;

    public string BuyerType { get; set; } = "individual";
    public string BuyerName { get; set; } = string.Empty;
    public string TaxCode { get; set; } = string.Empty;
    public string InvoiceAddress { get; set; } = string.Empty;
    public string InvoiceProvinceCity { get; set; } = string.Empty;
    public string InvoiceWard { get; set; } = string.Empty;
    public string IdentityNumber { get; set; } = string.Empty;
    public string PassportNumber { get; set; } = string.Empty;
    public string InvoiceEmail { get; set; } = string.Empty;
    public string InvoicePhone { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public string BankAccountNumber { get; set; } = string.Empty;

    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedDate { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}
