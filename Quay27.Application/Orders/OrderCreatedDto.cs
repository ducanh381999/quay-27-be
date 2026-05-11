namespace Quay27.Application.Orders;

public sealed class OrderCreatedDto
{
    public Guid Id { get; init; }
    public string Code { get; init; } = string.Empty;

    /// <summary>When set, <c>yyyy-MM-dd</c> (VN calendar) for deep-linking to the customer full sheet.</summary>
    public string? CustomerSheetDateIso { get; init; }
}
