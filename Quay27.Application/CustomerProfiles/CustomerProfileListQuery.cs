namespace Quay27.Application.CustomerProfiles;

/// <summary>
/// Query for paginated CRM customer profile list. Bound from GET query string.
/// </summary>
public sealed class CustomerProfileListQuery
{
    public string? Search { get; set; }

    /// <summary>Zero-based offset. Default 0.</summary>
    public int Skip { get; set; }

    /// <summary>Page size. Clamped server-side (1..200).</summary>
    public int Take { get; set; } = 50;

    /// <summary>active = not soft-deleted; inactive = soft-deleted only; all = both.</summary>
    public string? Status { get; set; }

    /// <summary>Repeat query param: customerGroup=A&amp;customerGroup=B — OR match on CustomerProfile.CustomerGroup (exact name).</summary>
    public List<string>? CustomerGroups { get; set; }

    public DateTime? CreatedFrom { get; set; }
    public DateTime? CreatedTo { get; set; }

    public string? BuyerType { get; set; }
    public string? Gender { get; set; }

    /// <summary>Case-insensitive contains on address / ward / province and invoice address fields (OR).</summary>
    public string? DeliveryAreaText { get; set; }
}
