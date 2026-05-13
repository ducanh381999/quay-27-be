namespace Quay27.Domain.Entities;

public class BankCatalogItem
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string? GlobalName { get; set; }
    public int OrderNum { get; set; }
    public string SearchText { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int? CountryId { get; set; }
}
