namespace Quay27.Application.Orders;

public sealed record PurchaseOrderListQuery(
    string? Search,
    IReadOnlyList<string>? Statuses,
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? DeliveryPartnerContains,
    DateTime? DeliveryFromUtc,
    DateTime? DeliveryToUtc,
    IReadOnlyList<string>? ProvinceKeys,
    IReadOnlyList<string>? PaymentMethods,
    IReadOnlyList<Guid>? CreatedByUserIds,
    IReadOnlyList<Guid>? ReceivedByUserIds,
    IReadOnlyList<Guid>? SaleChannelIds);

public sealed record SalesInvoiceListQuery(
    string? Search,
    IReadOnlyList<string>? InvoiceDeliveryTypes,
    IReadOnlyList<string>? Statuses,
    IReadOnlyList<string>? DeliveryStatuses,
    DateTime? FromUtc,
    DateTime? ToUtc,
    string? DeliveryPartnerContains,
    DateTime? DeliveryFromUtc,
    DateTime? DeliveryToUtc,
    IReadOnlyList<string>? ProvinceKeys,
    IReadOnlyList<string>? PaymentMethods,
    IReadOnlyList<Guid>? CreatedByUserIds,
    IReadOnlyList<Guid>? SellerUserIds,
    IReadOnlyList<Guid>? PriceListIds,
    IReadOnlyList<Guid>? SaleChannelIds);

public sealed record SalesReturnListQuery(
    string? Search,
    IReadOnlyList<string>? ReturnTypes,
    IReadOnlyList<string>? Statuses,
    DateTime? FromUtc,
    DateTime? ToUtc,
    IReadOnlyList<Guid>? CreatedByUserIds,
    IReadOnlyList<Guid>? ReceivedByUserIds,
    IReadOnlyList<Guid>? SaleChannelIds,
    IReadOnlyList<string>? OtherCollectionTypes);
