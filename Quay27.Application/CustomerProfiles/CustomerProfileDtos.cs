namespace Quay27.Application.CustomerProfiles;

public sealed class CustomerProfileDto
{
    public Guid Id { get; init; }
    public string CustomerCode { get; init; } = string.Empty;
    public string CustomerName { get; init; } = string.Empty;
    public string Phone1 { get; init; } = string.Empty;
    public string Phone2 { get; init; } = string.Empty;
    public DateOnly? Birthday { get; init; }
    public string Gender { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public string Facebook { get; init; } = string.Empty;
    public string Address { get; init; } = string.Empty;
    public string ProvinceCity { get; init; } = string.Empty;
    public string Ward { get; init; } = string.Empty;
    public string CustomerGroup { get; init; } = string.Empty;
    public string Note { get; init; } = string.Empty;
    public string BuyerType { get; init; } = "individual";
    public string BuyerName { get; init; } = string.Empty;
    public string TaxCode { get; init; } = string.Empty;
    public string InvoiceAddress { get; init; } = string.Empty;
    public string InvoiceProvinceCity { get; init; } = string.Empty;
    public string InvoiceWard { get; init; } = string.Empty;
    public string IdentityNumber { get; init; } = string.Empty;
    public string PassportNumber { get; init; } = string.Empty;
    public string InvoiceEmail { get; init; } = string.Empty;
    public string InvoicePhone { get; init; } = string.Empty;
    public string BankName { get; init; } = string.Empty;
    public string BankAccountNumber { get; init; } = string.Empty;
    public DateTime CreatedDate { get; init; }
    public string CreatedBy { get; init; } = string.Empty;
    public DateTime? UpdatedDate { get; init; }
    public string? UpdatedBy { get; init; }
}

public sealed class CreateCustomerProfileRequest
{
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
}

public sealed class PatchCustomerProfileRequest
{
    public string? CustomerName { get; set; }
    public string? Phone1 { get; set; }
    public string? Phone2 { get; set; }
    public DateOnly? Birthday { get; set; }
    public string? Gender { get; set; }
    public string? Email { get; set; }
    public string? Facebook { get; set; }
    public string? Address { get; set; }
    public string? ProvinceCity { get; set; }
    public string? Ward { get; set; }
    public string? CustomerGroup { get; set; }
    public string? Note { get; set; }
    public string? BuyerType { get; set; }
    public string? BuyerName { get; set; }
    public string? TaxCode { get; set; }
    public string? InvoiceAddress { get; set; }
    public string? InvoiceProvinceCity { get; set; }
    public string? InvoiceWard { get; set; }
    public string? IdentityNumber { get; set; }
    public string? PassportNumber { get; set; }
    public string? InvoiceEmail { get; set; }
    public string? InvoicePhone { get; set; }
    public string? BankName { get; set; }
    public string? BankAccountNumber { get; set; }
}
