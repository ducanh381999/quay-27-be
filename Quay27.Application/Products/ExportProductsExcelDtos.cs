namespace Quay27.Application.Products;

public sealed class ExportProductsExcelRequest
{
    public string? Search { get; set; }
    public string? GroupId { get; set; }
    public string? Stock { get; set; }
    public string? DirectSale { get; set; }
    public string? Status { get; set; }
    public DateTimeOffset? CreatedFrom { get; set; }
    public DateTimeOffset? CreatedTo { get; set; }
    public DateTimeOffset? ExpectedFrom { get; set; }
    public DateTimeOffset? ExpectedTo { get; set; }
    public IReadOnlyList<ExportProductsExcelColumn> Columns { get; set; } = Array.Empty<ExportProductsExcelColumn>();
}

public sealed class ExportProductsExcelColumn
{
    public string Key { get; set; } = "";
    public string HeaderName { get; set; } = "";
}
