namespace Quay27.Application.CustomerProfiles;

/// <summary>
/// One CRM profile row for the main grid, including aggregates from invoices/returns.
/// </summary>
public sealed class CustomerProfileListItemDto
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

    public bool IsActive { get; init; }

    public bool IsDeleted { get; init; }

    /// <summary>Sum of PaidAmount on non-cancelled sales invoices linked to this profile.</summary>
    public decimal TotalSales { get; init; }

    /// <summary><see cref="TotalSales"/> minus sum of Amount on non-cancelled returns linked to this profile.</summary>
    public decimal TotalSalesNet { get; init; }

    /// <summary>Sum of (Subtotal - Discount - Paid) on non-cancelled invoices — outstanding on invoices, not ledger truth for all debt types.</summary>
    public decimal CurrentDebt { get; init; }
}
