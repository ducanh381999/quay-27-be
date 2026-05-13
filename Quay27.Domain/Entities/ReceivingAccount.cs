using Quay27.Domain.Constants;

namespace Quay27.Domain.Entities;

public class ReceivingAccount
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string AccountNumber { get; set; } = string.Empty;
    public string BankName { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedDate { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedDate { get; set; }
    public string? UpdatedBy { get; set; }

    /// <summary>Bank or e-wallet (defaults to bank for legacy rows).</summary>
    public string AccountKind { get; set; } = TreasuryConstants.AccountKindBank;

    /// <summary>Catalog code (bank or wallet).</summary>
    public string? ProviderCode { get; set; }

    public string? Note { get; set; }

    /// <summary>System-wide vs branch scope (branch picker not implemented yet).</summary>
    public string ScopeKind { get; set; } = TreasuryConstants.ScopeSystemWide;

    public Guid? BranchId { get; set; }
}
