namespace Quay27.Application.Products;

public sealed class PriceListDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
    public string Status { get; set; } = "active";
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public PriceListFormulaDto Formula { get; set; } = new();
    public string SalesRuleMode { get; set; } = "allow";
    public PriceListScopeDto Scope { get; set; } = new();
}

public sealed class PriceListFormulaDto
{
    public string Source { get; set; } = "costPrice";
    public string Operation { get; set; } = "add";
    public decimal Value { get; set; }
    public string Unit { get; set; } = "vnd";
    public bool RoundEnabled { get; set; }
    public int RoundTo { get; set; } = 1;
}

public sealed class PriceListScopeDto
{
    public bool ApplyAllBranches { get; set; }
    public IReadOnlyList<string> BranchIds { get; set; } = Array.Empty<string>();
    public bool ApplyAllCustomerGroups { get; set; }
    public IReadOnlyList<string> CustomerGroupIds { get; set; } = Array.Empty<string>();
    public bool ApplyAllCashiers { get; set; }
    public IReadOnlyList<string> CashierIds { get; set; } = Array.Empty<string>();
}

public sealed class UpsertPriceListRequest
{
    public string Name { get; set; } = "";
    public string Status { get; set; } = "active";
    public DateTimeOffset StartAt { get; set; }
    public DateTimeOffset EndAt { get; set; }
    public PriceListFormulaDto Formula { get; set; } = new();
    public string SalesRuleMode { get; set; } = "allow";
    public PriceListScopeDto Scope { get; set; } = new();
}

public sealed class PriceListItemsQuery
{
    public IReadOnlyList<Guid> PriceListIds { get; set; } = Array.Empty<Guid>();
    public string? Search { get; set; }
    public string? GroupId { get; set; }
    public string? Stock { get; set; }
    public string? PriceOperator { get; set; }
    public string? ComparePrice { get; set; }
    public decimal? CompareValue { get; set; }
}

public sealed class PriceListItemDto
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = "";
    public string ProductName { get; set; } = "";
    public decimal CostPrice { get; set; }
    public decimal LastImportPrice { get; set; }
    public Dictionary<string, decimal> PricesByListId { get; set; } = new();
}

public sealed class AddProductsByGroupsRequest
{
    public IReadOnlyList<string> GroupIds { get; set; } = Array.Empty<string>();
    public bool IncludeDescendants { get; set; } = true;
}

public sealed class AddAllProductsRequest
{
    public bool Confirmed { get; set; }
}

public sealed class ApplyPriceFormulaRequest
{
    public string ApplyTo { get; set; } = "all";
    public Guid? ProductId { get; set; }
}

public sealed class PriceListImportRequest
{
    public byte[] FileBytes { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = string.Empty;
}

public sealed class PriceListImportResult
{
    public int TotalRows { get; set; }
    public int SuccessfulRows { get; set; }
    public int FailedRows { get; set; }
    public IReadOnlyList<PriceListImportError> Errors { get; set; } = Array.Empty<PriceListImportError>();
}

public sealed class PriceListImportError
{
    public int RowNumber { get; set; }
    public string? Field { get; set; }
    public string Message { get; set; } = string.Empty;
}
