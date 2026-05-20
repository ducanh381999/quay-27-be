using ClosedXML.Excel;

namespace Quay27.Application.Reports;

public static class EndOfDayProductExcelTemplateFiller
{
    private const string BranchLabel = "Chi nhánh trung tâm";

    public static byte[] FillVertical(
        string templatePath,
        IReadOnlyList<EndOfDayProductRowDto> rows,
        (DateTime FromUtc, DateTime ToUtc, string DateLabel) range)
    {
        using var workbook = new XLWorkbook(templatePath);
        var ws = workbook.Worksheet(1);
        var nowLocal = DateTime.Now;

        ws.Cell(1, 2).Value = $"Ngày lập: {nowLocal:dd/MM/yyyy HH:mm}";
        ws.Cell(4, 2).Value = $"Ngày bán: {range.DateLabel}";
        ws.Cell(5, 2).Value = $"Chi nhánh: {BranchLabel}";

        const int dataStartRow = 9;
        ClearRow(ws, dataStartRow);
        WriteRows(ws, dataStartRow, rows, horizontal: false);
        return Save(workbook);
    }

    public static byte[] FillHorizontal(
        string templatePath,
        IReadOnlyList<EndOfDayProductRowDto> rows,
        (DateTime FromUtc, DateTime ToUtc, string DateLabel) range)
    {
        using var workbook = new XLWorkbook(templatePath);
        var ws = workbook.Worksheet(1);
        var nowLocal = DateTime.Now;

        ws.Cell(2, 2).Value = $"Ngày lập: {nowLocal:dd/MM/yyyy HH:mm}";
        ws.Cell(5, 2).Value = $"Ngày bán: {range.DateLabel}";
        ws.Cell(6, 2).Value = $"Chi nhánh: {BranchLabel}";

        const int dataStartRow = 10;
        ClearRow(ws, dataStartRow);
        WriteRows(ws, dataStartRow, rows, horizontal: true);
        return Save(workbook);
    }

    private static void WriteRows(IXLWorksheet ws, int startRow, IReadOnlyList<EndOfDayProductRowDto> rows,
        bool horizontal)
    {
        var rowIndex = startRow;
        if (rows.Count == 0)
        {
            ws.Cell(rowIndex, horizontal ? 4 : 2).Value = "Báo cáo không có dữ liệu";
            return;
        }

        foreach (var row in rows)
        {
            if (horizontal)
            {
                ws.Cell(rowIndex, 2).Value = row.ProductCode;
                ws.Cell(rowIndex, 5).Value = row.ProductName;
                ws.Cell(rowIndex, 9).Value = row.SoldQuantity;
                ws.Cell(rowIndex, 10).Value = (double)row.ListPrice;
                ws.Cell(rowIndex, 10).Style.NumberFormat.Format = "#,##0";
                ws.Cell(rowIndex, 11).Value = (double)row.Revenue;
                ws.Cell(rowIndex, 11).Style.NumberFormat.Format = "#,##0";
                ws.Cell(rowIndex, 13).Value = (double)row.Variance;
                ws.Cell(rowIndex, 13).Style.NumberFormat.Format = "#,##0";
                ws.Cell(rowIndex, 15).Value = row.ReturnQuantity;
            }
            else
            {
                ws.Cell(rowIndex, 2).Value = row.ProductCode;
                ws.Cell(rowIndex, 4).Value = row.ProductName;
                ws.Cell(rowIndex, 8).Value = row.SoldQuantity;
                ws.Cell(rowIndex, 9).Value = (double)row.Revenue;
                ws.Cell(rowIndex, 9).Style.NumberFormat.Format = "#,##0";
                ws.Cell(rowIndex, 10).Value = row.ReturnQuantity;
                ws.Cell(rowIndex, 11).Value = (double)row.ReturnValue;
                ws.Cell(rowIndex, 11).Style.NumberFormat.Format = "#,##0";
                ws.Cell(rowIndex, 13).Value = (double)row.NetRevenue;
                ws.Cell(rowIndex, 13).Style.NumberFormat.Format = "#,##0";
            }

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
}
