using Quay27.Application.Cashbook;

namespace Quay27.Application.Abstractions;

public interface IPaymentCategoryService
{
    Task<IReadOnlyList<PaymentCategoryDto>> ListAsync(string? kind, CancellationToken cancellationToken = default);
}
