using Microsoft.Extensions.Logging;
using Quay27.Application.Abstractions;
using Quay27.Application.Repositories;

namespace Quay27.Infrastructure.Services;

public class EndOfDayService : IEndOfDayService
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<EndOfDayService> _logger;

    public EndOfDayService(ICustomerRepository customers, IUnitOfWork unitOfWork, ILogger<EndOfDayService> logger)
    {
        _customers = customers;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<int> RollUnqueuedCustomersToNextDayAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var moved = await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                var n = await _customers.AdvanceSheetDateForUnqueuedActiveCustomersAsync(cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
                return n;
            }, cancellationToken);

            if (moved > 0)
                _logger.LogInformation("End of day job moved {Count} customers to next sheet date.", moved);
            return moved;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "End of day job failed.");
            throw;
        }
    }
}
