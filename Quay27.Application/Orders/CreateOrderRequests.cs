namespace Quay27.Application.Orders;

public sealed class CreateOrderItemRequest
{
    public Guid ProductId { get; set; }
    public string ProductCode { get; set; } = string.Empty;
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public string? Note { get; set; }
}

public sealed class CreatePurchaseOrderRequest
{
    public Guid? CustomerProfileId { get; set; }
    public Guid? SellerUserId { get; set; }
    public Guid? SaleChannelId { get; set; }

    public Guid? PriceListId { get; set; }

    /// <summary>cash | wallet | transfer | card</summary>
    public string PaymentMethod { get; set; } = "cash";

    /// <summary>Required when payment is transfer, card, or wallet.</summary>
    public Guid? ReceivingAccountId { get; set; }

    public decimal AmountPaid { get; set; }

    public decimal ClientSubtotal { get; set; }
    public decimal ClientDiscountAmount { get; set; }
    public decimal ClientTotal { get; set; }

    public DateTime? ScheduledDeliveryUtc { get; set; }
    public string? Note { get; set; }

    public IList<CreateOrderItemRequest> Items { get; set; } = new List<CreateOrderItemRequest>();
}

public sealed class CreateSalesInvoiceRequest
{
    public Guid? PurchaseOrderId { get; set; }

    public Guid? CustomerProfileId { get; set; }
    public Guid? SellerUserId { get; set; }
    public Guid? SaleChannelId { get; set; }

    public Guid? PriceListId { get; set; }

    public string PaymentMethod { get; set; } = "cash";
    public Guid? ReceivingAccountId { get; set; }
    public decimal PaidAmount { get; set; }

    public decimal ClientSubtotal { get; set; }
    public decimal ClientDiscountAmount { get; set; }
    public decimal ClientTotal { get; set; }

    public string? Note { get; set; }

    public IList<CreateOrderItemRequest> Items { get; set; } = new List<CreateOrderItemRequest>();
}

public sealed class CreateSalesReturnRequest
{
    public Guid? CustomerProfileId { get; set; }
    public Guid? SellerUserId { get; set; }
    public Guid? SaleChannelId { get; set; }
    public Guid? PriceListId { get; set; }
    public string? Note { get; set; }

    public string? PaymentMethod { get; set; }
    public Guid? ReceivingAccountId { get; set; }
    public decimal PaidAmount { get; set; }

    public string? RefundPaymentMethod { get; set; }
    public Guid? RefundReceivingAccountId { get; set; }

    public IList<CreateOrderItemRequest> ReturnItems { get; set; } = new List<CreateOrderItemRequest>();

    public decimal ClientReturnSubtotal { get; set; }
    public decimal ClientReturnDiscountAmount { get; set; }
    public decimal ClientReturnFeeAmount { get; set; }
    public decimal ClientRefundDueAmount { get; set; }

    public IList<CreateOrderItemRequest> ExchangeItems { get; set; } = new List<CreateOrderItemRequest>();
    public bool ExchangeDelivery { get; set; }

    public decimal ClientExchangeSubtotal { get; set; }
    public decimal ClientExchangeDiscountAmount { get; set; }
    public decimal ClientPurchaseDueAmount { get; set; }
    public decimal ClientNetAmountDueFromCustomer { get; set; }
}
