namespace Quay27.Domain.Entities;

public sealed class PurchaseOrder
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }

    public Guid? CustomerProfileId { get; set; }
    public CustomerProfile? CustomerProfile { get; set; }

    /// <summary>draft | confirmed | shipping | completed | cancelled</summary>
    public string Status { get; set; } = "draft";

    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }

    public decimal AmountDue { get; set; }
    public decimal AmountPaid { get; set; }

    public string? Note { get; set; }

    public string? DeliveryPartner { get; set; }
    public DateTime? DeliveryFromUtc { get; set; }
    public DateTime? DeliveryToUtc { get; set; }

    public string? ProvinceKey { get; set; }
    public string? DistrictKey { get; set; }

    /// <summary>cash | wallet | transfer | card</summary>
    public string PaymentMethod { get; set; } = "cash";

    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public Guid? ReceivedByUserId { get; set; }
    public User? ReceivedByUser { get; set; }

    public Guid? SellerUserId { get; set; }
    public User? SellerUser { get; set; }

    public Guid? SaleChannelId { get; set; }
    public SaleChannel? SaleChannel { get; set; }

    public ICollection<PurchaseOrderItem> Items { get; set; } = new List<PurchaseOrderItem>();
}
