using Quay27.Domain.Entities;

namespace Quay27.Application.Repositories;

public interface IPaymentCategoryRepository
{
    Task<IReadOnlyList<PaymentCategory>> ListAsync(string? kind, CancellationToken cancellationToken = default);
    Task<PaymentCategory?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<PaymentCategory?> GetByCodeAsync(string code, CancellationToken cancellationToken = default);
}
