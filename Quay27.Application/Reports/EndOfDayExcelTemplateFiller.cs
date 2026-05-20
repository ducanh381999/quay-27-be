using ClosedXML.Excel;

namespace Quay27.Application.Reports;

public static class EndOfDayExcelTemplateFiller
{
    private const string BranchLabel = "Chi nhánh trung tâm";

    public static byte[] FillVertical(string templatePath, EndOfDayReportQuery query,
        IReadOnlyList<EndOfDaySalesRowDto> rows, (DateTime FromUtc, DateTime ToUtc, string DateLabel) range)
    {
        using var workbook = new XLWorkbook(templatePath);
        var ws = workbook.Worksheet(1);
        var nowLocal = DateTime.Now;

        ws.Cell(1, 2).Value = $"Ngày lập: {nowLocal:dd/MM/yyyy HH:mm}";
        ws.Cell(4, 2).Value = $"Ngày bán:  {range.DateLabel}";
        ws.Cell(5, 2).Value = $"Ngày thanh toán: {range.DateLabel}";
        ws.Cell(6, 2).Value = $"Chi nhánh: {BranchLabel}";

        const int dataStartRow = 10;
        ClearPlaceholderRow(ws, dataStartRow);
        var rowIndex = dataStartRow;

        if (rows.Count == 0)
        {
            ws.Cell(rowIndex, 2).Value = "Báo cáo không có dữ liệu";
        }
        else
        {
            foreach (var row in rows)
            {
                ws.Cell(rowIndex, 2).Value = row.Code;
                ws.Cell(rowIndex, 3).Value = ToLocalDateTime(row.CreatedAtUtc);
                ws.Cell(rowIndex, 3).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                ws.Cell(rowIndex, 5).Value = row.Quantity;
                ws.Cell(rowIndex, 7).Value = (double)row.SubtotalAmount;
                ws.Cell(rowIndex, 7).Style.NumberFormat.Format = "#,##0";
                ws.Cell(rowIndex, 8).Value = 0d;
                ws.Cell(rowIndex, 9).Value = 0d;
                ws.Cell(rowIndex, 10).Value = 0d;
                ws.Cell(rowIndex, 11).Value = 0d;
                ws.Cell(rowIndex, 13).Value = (double)row.PaidAmount;
                ws.Cell(rowIndex, 13).Style.NumberFormat.Format = "#,##0";
                rowIndex++;
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] FillHorizontal(string templatePath, EndOfDayReportQuery query,
        IReadOnlyList<EndOfDaySalesRowDto> rows, (DateTime FromUtc, DateTime ToUtc, string DateLabel) range)
    {
        using var workbook = new XLWorkbook(templatePath);
        var ws = workbook.Worksheet(1);
        var nowLocal = DateTime.Now;

        ws.Cell(2, 2).Value = $"Ngày lập: {nowLocal:dd/MM/yyyy HH:mm}";
        ws.Cell(5, 2).Value = $"Ngày bán:  {range.DateLabel}";
        ws.Cell(6, 2).Value = $"Ngày thanh toán:  {range.DateLabel}";
        ws.Cell(7, 2).Value = $"Chi nhánh: {BranchLabel}";

        const int dataStartRow = 12;
        ClearPlaceholderRow(ws, dataStartRow);
        var rowIndex = dataStartRow;

        if (rows.Count == 0)
        {
            ws.Cell(rowIndex, 2).Value = "Báo cáo không có dữ liệu";
        }
        else
        {
            foreach (var row in rows)
            {
                ws.Cell(rowIndex, 2).Value = row.Code;
                ws.Cell(rowIndex, 3).Value = row.CustomerName ?? string.Empty;
                ws.Cell(rowIndex, 4).Value = row.SellerDisplayName ?? string.Empty;
                ws.Cell(rowIndex, 7).Value = ToLocalDateTime(row.CreatedAtUtc);
                ws.Cell(rowIndex, 7).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                ws.Cell(rowIndex, 8).Value = row.Quantity;
                ws.Cell(rowIndex, 9).Value = (double)row.SubtotalAmount;
                ws.Cell(rowIndex, 9).Style.NumberFormat.Format = "#,##0";
                ws.Cell(rowIndex, 10).Value = (double)row.DiscountAmount;
                ws.Cell(rowIndex, 10).Style.NumberFormat.Format = "#,##0";
                ws.Cell(rowIndex, 12).Value = 0d;
                ws.Cell(rowIndex, 13).Value = 0d;
                ws.Cell(rowIndex, 14).Value = 0d;
                rowIndex++;
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void ClearPlaceholderRow(IXLWorksheet ws, int row)
    {
        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 14;
        for (var c = 1; c <= lastCol; c++)
            ws.Cell(row, c).Clear(XLClearOptions.Contents);
    }

    private static DateTime ToLocalDateTime(DateTime utc) =>
        utc.Kind == DateTimeKind.Utc ? utc.ToLocalTime() : utc;
}
