namespace Quay27.Application.CustomerProfiles;

public sealed record CustomerDeliveryAddressDto(
    Guid Id,
    string AddressName,
    string RecipientName,
    string Phone,
    string AddressLine,
    string ProvinceCity,
    string Ward,
    DateTime CreatedAtUtc);

public sealed record CreateCustomerDeliveryAddressRequest(
    string AddressName,
    string RecipientName,
    string Phone,
    string AddressLine,
    string ProvinceCity,
    string Ward);

public sealed record PatchCustomerDeliveryAddressRequest(
    string? AddressName,
    string? RecipientName,
    string? Phone,
    string? AddressLine,
    string? ProvinceCity,
    string? Ward);
