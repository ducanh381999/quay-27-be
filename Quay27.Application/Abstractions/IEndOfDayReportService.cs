using Quay27.Application.Reports;

namespace Quay27.Application.Abstractions;

public interface IEndOfDayReportService
{
    Task<byte[]> GenerateSalesPdfAsync(EndOfDayReportQuery query, CancellationToken cancellationToken = default);
}
