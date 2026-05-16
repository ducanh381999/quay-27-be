using ClosedXML.Excel;

namespace Quay27.Application.Reports;

public static class EndOfDayCashFlowExcelTemplateFiller
{
    private const string BranchLabel = "Chi nhánh trung tâm";

    public static byte[] FillHorizontal(
        string templatePath,
        IReadOnlyList<EndOfDayCashflowRowDto> rows,
        (DateTime FromUtc, DateTime ToUtc, string DateLabel) range)
    {
        using var workbook = new XLWorkbook(templatePath);
        var ws = workbook.Worksheet(1);
        var nowLocal = DateTime.Now;

        ws.Cell(3, 2).Value = $"Ngày lập: {nowLocal:dd/MM/yyyy HH:mm}";
        ws.Cell(5, 2).Value = $"Từ ngày {range.DateLabel} đến ngày {range.DateLabel}";
        ws.Cell(6, 2).Value = $"Chi nhánh: {BranchLabel}";

        const int dataStartRow = 11;
        ClearRow(ws, dataStartRow);
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
                ws.Cell(rowIndex, 3).Value = row.CategoryName ?? string.Empty;
                ws.Cell(rowIndex, 5).Value = row.StaffDisplayName ?? string.Empty;
                ws.Cell(rowIndex, 8).Value = row.CounterpartyName ?? string.Empty;
                ws.Cell(rowIndex, 9).Value = row.EntryType == "Receipt" ? "Thu" : "Chi";
                ws.Cell(rowIndex, 10).Value = ToLocalDateTime(row.OccurredAtUtc);
                ws.Cell(rowIndex, 10).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
                ws.Cell(rowIndex, 12).Value = (double)row.Amount;
                ws.Cell(rowIndex, 12).Style.NumberFormat.Format = "#,##0";
                ws.Cell(rowIndex, 13).Value = row.SourceCode ?? string.Empty;
                rowIndex++;
            }
        }

        return Save(workbook);
    }

    public static byte[] FillVerticalCashflow(
        string templatePath,
        IReadOnlyList<EndOfDayCashflowAggregateRowDto> rows,
        (DateTime FromUtc, DateTime ToUtc, string DateLabel) range,
        string title)
    {
        using var workbook = new XLWorkbook(templatePath);
        var ws = workbook.Worksheet(1);
        var nowLocal = DateTime.Now;

        ws.Cell(1, 2).Value = $"Ngày lập: {nowLocal:dd/MM/yyyy HH:mm}";
        ws.Cell(2, 6).Value = title;
        ws.Cell(4, 2).Value = $"Ngày bán: {range.DateLabel}";
        ws.Cell(5, 2).Value = $"Chi nhánh: {BranchLabel}";

        const int dataStartRow = 11;
        ClearRow(ws, dataStartRow);
        var rowIndex = dataStartRow;

        if (rows.Count == 0)
        {
            ws.Cell(rowIndex, 2).Value = "Báo cáo không có dữ liệu";
        }
        else
        {
            foreach (var row in rows)
            {
                ws.Cell(rowIndex, 2).Value = row.Label;
                ws.Cell(rowIndex, 5).Value = (double)row.CashAmount;
                ws.Cell(rowIndex, 5).Style.NumberFormat.Format = "#,##0";
                ws.Cell(rowIndex, 10).Value = (double)row.TransferAmount;
                ws.Cell(rowIndex, 10).Style.NumberFormat.Format = "#,##0";
                ws.Cell(rowIndex, 13).Value = (double)row.CardAmount;
                ws.Cell(rowIndex, 13).Style.NumberFormat.Format = "#,##0";
                rowIndex++;
            }
        }

        return Save(workbook);
    }

    public static byte[] FillSummary(
        string templatePath,
        EndOfDaySummaryDto summary,
        (DateTime FromUtc, DateTime ToUtc, string DateLabel) range)
    {
        using var workbook = new XLWorkbook(templatePath);
        var ws = workbook.Worksheet(1);
        var nowLocal = DateTime.Now;

        ws.Cell(1, 2).Value = $"Ngày lập: {nowLocal:dd/MM/yyyy HH:mm}";
        ws.Cell(4, 2).Value = $"Ngày bán: {range.DateLabel}";
        ws.Cell(5, 2).Value = $"Chi nhánh: {BranchLabel}";

        WriteAggregateBlock(ws, 11, summary.CashflowRows);

        const int salesValueStart = 18;
        ClearRow(ws, salesValueStart);
        var salesRow = salesValueStart;
        if (summary.SalesValueRows.Count == 0)
        {
            ws.Cell(salesRow, 2).Value = "Báo cáo không có dữ liệu";
        }
        else
        {
            foreach (var row in summary.SalesValueRows)
            {
                ws.Cell(salesRow, 2).Value = row.Label;
                ws.Cell(salesRow, 3).Value = (double)row.Value;
                ws.Cell(salesRow, 3).Style.NumberFormat.Format = "#,##0";
                ws.Cell(salesRow, 7).Value = (double)row.CashAmount;
                ws.Cell(salesRow, 7).Style.NumberFormat.Format = "#,##0";
                ws.Cell(salesRow, 11).Value = (double)row.TransferAmount;
                ws.Cell(salesRow, 11).Style.NumberFormat.Format = "#,##0";
                ws.Cell(salesRow, 14).Value = (double)row.CardAmount;
                ws.Cell(salesRow, 14).Style.NumberFormat.Format = "#,##0";
                salesRow++;
            }
        }

        const int salesCountStart = 27;
        ClearRow(ws, salesCountStart);
        var countRow = salesCountStart;
        if (summary.SalesCountRows.Count == 0)
        {
            ws.Cell(countRow, 2).Value = "Báo cáo không có dữ liệu";
        }
        else
        {
            foreach (var row in summary.SalesCountRows)
            {
                ws.Cell(countRow, 2).Value = row.Label;
                ws.Cell(countRow, 4).Value = row.TransactionCount;
                ws.Cell(countRow, 9).Value = row.CashAmount;
                ws.Cell(countRow, 12).Value = row.TransferAmount;
                countRow++;
            }
        }

        return Save(workbook);
    }

    private static void WriteAggregateBlock(IXLWorksheet ws, int startRow,
        IReadOnlyList<EndOfDayCashflowAggregateRowDto> rows)
    {
        ClearRow(ws, startRow);
        var rowIndex = startRow;
        if (rows.Count == 0)
        {
            ws.Cell(rowIndex, 2).Value = "Báo cáo không có dữ liệu";
            return;
        }

        foreach (var row in rows)
        {
            ws.Cell(rowIndex, 2).Value = row.Label;
            ws.Cell(rowIndex, 5).Value = (double)row.CashAmount;
            ws.Cell(rowIndex, 5).Style.NumberFormat.Format = "#,##0";
            ws.Cell(rowIndex, 10).Value = (double)row.TransferAmount;
            ws.Cell(rowIndex, 10).Style.NumberFormat.Format = "#,##0";
            ws.Cell(rowIndex, 13).Value = (double)row.CardAmount;
            ws.Cell(rowIndex, 13).Style.NumberFormat.Format = "#,##0";
            rowIndex++;
        }
    }

    private static void ClearRow(IXLWorksheet ws, int row)
    {
        var lastCol = ws.LastColumnUsed()?.ColumnNumber() ?? 15;
        for (var c = 1; c <= lastCol; c++)
            ws.Cell(row, c).Clear(XLClearOptions.Contents);
    }

    private static byte[] Save(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static DateTime ToLocalDateTime(DateTime utc) =>
        utc.Kind == DateTimeKind.Utc ? utc.ToLocalTime() : utc;
}
