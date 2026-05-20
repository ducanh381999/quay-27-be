namespace Quay27.Domain.Entities;

public sealed class SalesInvoice
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>Mã tham chiếu trả hàng (hiển thị trên lưới).</summary>
    public string? ReturnReferenceCode { get; set; }

    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }

    public Guid? CustomerProfileId { get; set; }
    public CustomerProfile? CustomerProfile { get; set; }

    public string? Note { get; set; }

    /// <summary>no_delivery | delivery</summary>
    public string InvoiceDeliveryType { get; set; } = "no_delivery";

    /// <summary>processing | completed | undeliverable | cancelled</summary>
    public string Status { get; set; } = "processing";

    public string? DeliveryStatus { get; set; }

    public string? DeliveryPartner { get; set; }
    public DateTime? DeliveryFromUtc { get; set; }
    public DateTime? DeliveryToUtc { get; set; }

    public string? ProvinceKey { get; set; }
    public string? DistrictKey { get; set; }

    /// <summary>cash | wallet | transfer | card</summary>
    public string PaymentMethod { get; set; } = "cash";

    public decimal SubtotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal PaidAmount { get; set; }

    public Guid? SellerUserId { get; set; }
    public User? SellerUser { get; set; }

    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public Guid? PriceListId { get; set; }
    public PriceList? PriceList { get; set; }

    public Guid? ReceivingAccountId { get; set; }
    public ReceivingAccount? ReceivingAccount { get; set; }

    public Guid? SaleChannelId { get; set; }
    public SaleChannel? SaleChannel { get; set; }

    public Guid? PurchaseOrderId { get; set; }
    public PurchaseOrder? PurchaseOrder { get; set; }

    public ICollection<SalesInvoiceItem> Items { get; set; } = new List<SalesInvoiceItem>();
}
