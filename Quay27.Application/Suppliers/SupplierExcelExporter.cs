using ClosedXML.Excel;

namespace Quay27.Application.Suppliers;

/// <summary>
/// Xuất danh sách nhà cung cấp ra Excel theo format DanhSachNhaCungCap_KV*.xlsx.
/// </summary>
internal static class SupplierExcelExporter
{
    public static byte[] Build(IReadOnlyList<SupplierDto> suppliers, DateTime exportedAt)
    {
        using var workbook = new XLWorkbook();
        var sheetName = $"DanhSachNhaCungCap_KV{exportedAt:ddMMyyyy-HHmmss}";
        if (sheetName.Length > 31)
            sheetName = sheetName[..31];

        var ws = workbook.Worksheets.Add(sheetName);

        var headers = new[]
        {
            "Mã nhà cung cấp",
            "Tên nhà cung cấp",
            "Email",
            "Điện thoại",
            "Địa chỉ",
            "Khu vực",
            "Phường/Xã",
            "Tổng mua",
            "Nợ cần trả hiện tại",
            "Mã số thuế",
            "Ghi chú",
            "Nhóm nhà cung cấp",
            "Trạng thái",
            "Tổng mua trừ trả hàng",
            "Công ty",
            "Người tạo",
            "Ngày tạo",
        };

        for (var i = 0; i < headers.Length; i++)
        {
            ws.Cell(1, i + 1).Value = headers[i];
        }

        var rowIndex = 2;
        foreach (var s in suppliers)
        {
            ws.Cell(rowIndex, 1).Value = s.Code;
            ws.Cell(rowIndex, 2).Value = s.Name;
            ws.Cell(rowIndex, 3).Value = s.Email;
            ws.Cell(rowIndex, 4).Value = s.Phone;
            ws.Cell(rowIndex, 5).Value = s.Address;
            ws.Cell(rowIndex, 6).Value = s.Region;
            ws.Cell(rowIndex, 7).Value = s.Ward;
            ws.Cell(rowIndex, 8).Value = (double)s.TotalPurchase;
            ws.Cell(rowIndex, 9).Value = (double)s.CurrentDebt;
            ws.Cell(rowIndex, 10).Value = s.TaxCode;
            ws.Cell(rowIndex, 11).Value = s.Notes;
            ws.Cell(rowIndex, 12).Value = s.SupplierGroupName ?? string.Empty;
            ws.Cell(rowIndex, 13).Value = s.IsActive ? 1 : 0;
            ws.Cell(rowIndex, 14).Value = (double)(s.TotalPurchase - s.TotalReturn);
            ws.Cell(rowIndex, 15).Value = s.CompanyName;
            ws.Cell(rowIndex, 16).Value = s.CreatedBy;
            ws.Cell(rowIndex, 17).Value = s.CreatedDate;
            ws.Cell(rowIndex, 17).Style.NumberFormat.Format = "dd/MM/yyyy HH:mm";
            ws.Cell(rowIndex, 8).Style.NumberFormat.Format = "#,##0";
            ws.Cell(rowIndex, 9).Style.NumberFormat.Format = "#,##0";
            ws.Cell(rowIndex, 14).Style.NumberFormat.Format = "#,##0";
            rowIndex++;
        }

        var lastRow = Math.Max(rowIndex - 1, 1);
        var range = ws.Range(1, 1, lastRow, headers.Length);
        var table = range.CreateTable("supplierTable");
        table.Theme = XLTableTheme.TableStyleMedium2;
        table.ShowAutoFilter = true;
        table.HeadersRow().Style.Font.Bold = true;

        ws.Columns().AdjustToContents();

        using var ms = new MemoryStream();
        workbook.SaveAs(ms);
        return ms.ToArray();
    }
}
