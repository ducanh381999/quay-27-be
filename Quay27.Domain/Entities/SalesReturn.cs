namespace Quay27.Domain.Entities;

public sealed class SalesReturn
{
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }

    public string? CustomerCode { get; set; }
    public string? CustomerName { get; set; }

    public Guid? CustomerProfileId { get; set; }
    public CustomerProfile? CustomerProfile { get; set; }

    /// <summary>by_invoice | quick_return | refund_transfer</summary>
    public string ReturnType { get; set; } = "by_invoice";

    /// <summary>returned | cancelled</summary>
    public string Status { get; set; } = "returned";

    public Guid? CreatedByUserId { get; set; }
    public User? CreatedByUser { get; set; }

    public Guid? ReceivedByUserId { get; set; }
    public User? ReceivedByUser { get; set; }

    public Guid? SaleChannelId { get; set; }
    public SaleChannel? SaleChannel { get; set; }

    public Guid? SellerUserId { get; set; }
    public User? SellerUser { get; set; }

    public Guid? PriceListId { get; set; }
    public PriceList? PriceList { get; set; }

    /// <summary>cash | wallet | transfer | card — thu thêm từ khách khi có đổi hàng.</summary>
    public string? PaymentMethod { get; set; }

    public Guid? ReceivingAccountId { get; set; }
    public ReceivingAccount? ReceivingAccount { get; set; }

    public decimal PaidAmount { get; set; }

    /// <summary>cash | wallet | transfer | card — hoàn cho khách.</summary>
    public string? RefundPaymentMethod { get; set; }

    public Guid? RefundReceivingAccountId { get; set; }
    public ReceivingAccount? RefundReceivingAccount { get; set; }

    public string? OtherCollectionType { get; set; }

    public decimal ReturnSubtotalAmount { get; set; }
    public decimal ReturnDiscountAmount { get; set; }
    public decimal ReturnFeeAmount { get; set; }
    public decimal RefundDueAmount { get; set; }

    public bool HasExchangeItems { get; set; }
    public bool ExchangeDelivery { get; set; }
    public decimal ExchangeSubtotalAmount { get; set; }
    public decimal ExchangeDiscountAmount { get; set; }
    public decimal PurchaseDueAmount { get; set; }
    public decimal NetAmountDueFromCustomer { get; set; }

    public string? Note { get; set; }

    /// <summary>Số tiền tóm tắt trên lưới (refund khi không đổi hàng; net khi có đổi).</summary>
    public decimal Amount { get; set; }

    public ICollection<SalesReturnItem> ReturnItems { get; set; } = new List<SalesReturnItem>();
    public ICollection<SalesReturnExchangeItem> ExchangeItems { get; set; } = new List<SalesReturnExchangeItem>();
}
