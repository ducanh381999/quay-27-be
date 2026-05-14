using Quay27.Application.CustomerProfiles;

namespace Quay27.Application.Abstractions;

public interface ICustomerReceivableService
{
    Task<PagedReceivableTransactionsResult> ListTransactionsAsync(Guid customerProfileId, int skip, int take,
        string? kind, CancellationToken cancellationToken = default);

    Task<CustomerReceivableTransactionDto> RecordPaymentAsync(Guid customerProfileId,
        RecordCustomerPaymentRequest request, CancellationToken cancellationToken = default);

    Task<CustomerReceivableTransactionDto> RecordAdjustmentAsync(Guid customerProfileId,
        RecordCustomerAdjustmentRequest request, CancellationToken cancellationToken = default);

    Task<CustomerReceivableTransactionDto> RecordPaymentDiscountAsync(Guid customerProfileId,
        RecordCustomerPaymentDiscountRequest request, CancellationToken cancellationToken = default);

    Task<CustomerReceivableTransactionDto> RecordQrPaymentAsync(Guid customerProfileId,
        RecordCustomerQrPaymentRequest request, CancellationToken cancellationToken = default);
}
