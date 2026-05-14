using ClosedXML.Excel;

namespace Quay27.Application.Suppliers;

public static class SupplierPayableExcelExporter
{
    public static byte[] BuildTransactionsReport(
        string supplierCode,
        string supplierName,
        IReadOnlyList<SupplierPayableTransactionDto> rows,
        DateTime exportedAtUtc)
    {
        using var workbook = new XLWorkbook();
        var sheetName = "CongNoNCC";
        var ws = workbook.Worksheets.Add(sheetName);
        ws.Cell(1, 1).Value = "Mã NCC";
        ws.Cell(1, 2).Value = supplierCode;
        ws.Cell(2, 1).Value = "Tên NCC";
        ws.Cell(2, 2).Value = supplierName;
        ws.Cell(3, 1).Value = "Xuất lúc (UTC)";
        ws.Cell(3, 2).Value = exportedAtUtc;

        var headers = new[]
        {
            "Mã phiếu",
            "Thời gian",
            "Loại",
            "Giá trị",
            "Ảnh hưởng nợ phải trả",
        };
        var startRow = 5;
        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(startRow, i + 1).Value = headers[i];
        }

        var r = startRow + 1;
        foreach (var x in rows)
        {
            ws.Cell(r, 1).Value = x.Code;
            ws.Cell(r, 2).Value = x.OccurredAtUtc;
            ws.Cell(r, 2).Style.DateFormat.Format = "dd/MM/yyyy HH:mm";
            ws.Cell(r, 3).Value = x.TransactionTypeLabel;
            ws.Cell(r, 4).Value = (double)x.ValueAmount;
            ws.Cell(r, 5).Value = (double)x.PayableDebtImpact;
            ws.Cell(r, 4).Style.NumberFormat.Format = "#,##0.##";
            ws.Cell(r, 5).Style.NumberFormat.Format = "#,##0.##";
            r++;
        }

        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    public static byte[] BuildSupplierDebtSnapshot(
        string supplierCode,
        string supplierName,
        decimal currentDebt,
        DateTime exportedAtUtc)
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("TomTatNo");
        ws.Cell(1, 1).Value = "Mã nhà cung cấp";
        ws.Cell(1, 2).Value = supplierCode;
        ws.Cell(2, 1).Value = "Tên nhà cung cấp";
        ws.Cell(2, 2).Value = supplierName;
        ws.Cell(3, 1).Value = "Nợ cần trả hiện tại";
        ws.Cell(3, 2).Value = (double)currentDebt;
        ws.Cell(3, 2).Style.NumberFormat.Format = "#,##0.##";
        ws.Cell(4, 1).Value = "Xuất lúc (UTC)";
        ws.Cell(4, 2).Value = exportedAtUtc;
        ws.Columns().AdjustToContents();
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
