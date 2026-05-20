using ClosedXML.Excel;

namespace Quay27.Application.Reports;

/// <summary>Minimal Excel export for list-style reports (header block + column headers + rows).</summary>
public static class GenericListReportExcelFiller
{
    public static byte[] Fill(
        string templatePath,
        string reportTitle,
        ReportMetaDto meta,
        IReadOnlyList<string> columnHeaders,
        IReadOnlyList<IReadOnlyList<object?>> dataRows,
        int headerRow = 6,
        int dataStartRow = 7)
    {
        using var workbook = File.Exists(templatePath)
            ? new XLWorkbook(templatePath)
            : CreateWorkbook(columnHeaders, headerRow, dataStartRow);

        var ws = workbook.Worksheet(1);
        ws.Cell(1, 1).Value = reportTitle;
        ws.Cell(2, 1).Value = $"Ngày lập: {meta.GeneratedAtLocal:dd/MM/yyyy HH:mm}";
        ws.Cell(3, 1).Value = $"Thời gian: {meta.DateLabel}";
        ws.Cell(4, 1).Value = $"Chi nhánh: {meta.BranchLabel}";

        for (var c = 0; c < columnHeaders.Count; c++)
            ws.Cell(headerRow, c + 1).Value = columnHeaders[c];

        var rowIndex = dataStartRow;
        if (dataRows.Count == 0)
        {
            ws.Cell(rowIndex, 1).Value = "Báo cáo không có dữ liệu";
        }
        else
        {
            foreach (var row in dataRows)
            {
                for (var c = 0; c < row.Count; c++)
                    SetCellValue(ws.Cell(rowIndex, c + 1), row[c]);
                rowIndex++;
            }
        }

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static void SetCellValue(IXLCell cell, object? value)
    {
        switch (value)
        {
            case null:
                cell.Value = string.Empty;
                break;
            case string s:
                cell.Value = s;
                break;
            case DateTime dt:
                cell.Value = dt;
                break;
            case int i:
                cell.Value = i;
                break;
            case long l:
                cell.Value = l;
                break;
            case decimal m:
                cell.Value = (double)m;
                cell.Style.NumberFormat.Format = "#,##0.##";
                break;
            case double d:
                cell.Value = d;
                break;
            case float f:
                cell.Value = f;
                break;
            default:
                cell.Value = value.ToString() ?? string.Empty;
                break;
        }
    }

    private static XLWorkbook CreateWorkbook(IReadOnlyList<string> headers, int headerRow, int dataStartRow)
    {
        var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("BaoCao");
        for (var c = 0; c < headers.Count; c++)
            ws.Cell(headerRow, c + 1).Value = headers[c];
        return wb;
    }
}
