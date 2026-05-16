using Microsoft.Extensions.Logging;
using Quay27.Application.Abstractions;
using Quay27.Application.Reports;
using Quay27.Application.Repositories;

namespace Quay27.Infrastructure.Services;

public sealed class EndOfDayReportService : IEndOfDayReportService
{
    private readonly ISalesInvoiceRepository _invoices;
    private readonly IExcelToPdfConverter _pdfConverter;
    private readonly ILogger<EndOfDayReportService> _logger;

    public EndOfDayReportService(
        ISalesInvoiceRepository invoices,
        IExcelToPdfConverter pdfConverter,
        ILogger<EndOfDayReportService> logger)
    {
        _invoices = invoices;
        _pdfConverter = pdfConverter;
        _logger = logger;
    }

    public async Task<byte[]> GenerateSalesPdfAsync(
        EndOfDayReportQuery query,
        CancellationToken cancellationToken = default)
    {
        if (!string.Equals(query.Concern, "sales", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Chỉ hỗ trợ mối quan tâm bán hàng cho báo cáo cuối ngày.");

        var range = EndOfDayReportTimeRange.Resolve(query);
        var rows = await _invoices.ListForEndOfDayReportAsync(query, range.FromUtc, range.ToUtc, cancellationToken);

        var isHorizontal = string.Equals(query.DisplayMode, "horizontal", StringComparison.OrdinalIgnoreCase);
        var templateName = isHorizontal ? "EndOfDayDocumentLC.xlsx" : "EndOfDayDocument.xlsx";
        var templatePath = ResolveTemplatePath(templateName);

        if (!File.Exists(templatePath))
            throw new FileNotFoundException($"Không tìm thấy template báo cáo: {templateName}", templatePath);

        _logger.LogInformation(
            "Generating end-of-day sales report {Template} with {RowCount} rows ({FromUtc} - {ToUtc})",
            templateName,
            rows.Count,
            range.FromUtc,
            range.ToUtc);

        var xlsxBytes = isHorizontal
            ? EndOfDayExcelTemplateFiller.FillHorizontal(templatePath, query, rows, range)
            : EndOfDayExcelTemplateFiller.FillVertical(templatePath, query, rows, range);

        return await _pdfConverter.ConvertXlsxToPdfAsync(xlsxBytes, cancellationToken);
    }

    private static string ResolveTemplatePath(string templateName)
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Templates", "Reports", templateName),
            Path.Combine(Directory.GetCurrentDirectory(), "Templates", "Reports", templateName),
        };
        return candidates.FirstOrDefault(File.Exists) ?? candidates[0];
    }
}
