namespace Quay27.Domain.Entities;

public sealed class CustomerDeliveryAddress
{
    public Guid Id { get; set; }
    public Guid CustomerProfileId { get; set; }
    public CustomerProfile? CustomerProfile { get; set; }

    public string AddressName { get; set; } = string.Empty;
    public string RecipientName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string AddressLine { get; set; } = string.Empty;
    public string ProvinceCity { get; set; } = string.Empty;
    public string Ward { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime? UpdatedAtUtc { get; set; }
    public string? UpdatedBy { get; set; }
    public bool IsDeleted { get; set; }
}
