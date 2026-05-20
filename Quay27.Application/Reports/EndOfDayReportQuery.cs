namespace Quay27.Application.Reports;

public sealed record EndOfDayReportQuery(
    string DisplayMode,
    string Concern,
    string TimeMode,
    string? SingleDate,
    string? TimeFrom,
    string? TimeTo,
    string? CustomDateFrom,
    string? CustomDateTo,
    string? CustomerSearch,
    Guid? SellerUserId,
    Guid? CreatedByUserId,
    string? PaymentMethod,
    Guid? SaleChannelId,
    bool GroupSameProducts = false,
    string? ProductSearch = null,
    string? ProductType = null,
    Guid? ProductGroupId = null);
