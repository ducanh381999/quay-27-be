using Quay27.Application.Orders;

namespace Quay27.Application.Abstractions;

public interface ISaleChannelService
{
    Task<IReadOnlyList<SaleChannelDto>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default);
    Task<SaleChannelDto> CreateAsync(CreateSaleChannelRequest request, CancellationToken cancellationToken = default);
}
