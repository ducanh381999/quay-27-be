using ClosedXML.Excel;

namespace Quay27.Application.Products;

public sealed class ImportProductsExcelRequest
{
    public byte[] FileBytes { get; set; } = Array.Empty<byte>();
    public string FileName { get; set; } = "";
    public string DuplicateCodeConflictAction { get; set; } = ProductImportConflictAction.Error;
    public string DuplicateBarcodeConflictAction { get; set; } = ProductImportConflictAction.Error;
    public bool UpdateStock { get; set; }
    public bool UpdateCostPrice { get; set; }
    public bool UpdateDescription { get; set; }
}

public sealed class ImportProductsExcelResult
{
    public int TotalRows { get; set; }
    public int ImportedCount { get; set; }
    public int UpdatedCount { get; set; }
    public int SkippedCount { get; set; }
    public int FailedCount { get; set; }
    public IReadOnlyList<string> Errors { get; set; } = Array.Empty<string>();
}

public static class ProductImportConflictAction
{
    public const string Error = "error";
    public const string Replace = "replace";
}

public static class ProductImportTemplateBuilder
{
    public static byte[] Build()
    {
        using var workbook = new XLWorkbook();
        var ws = workbook.Worksheets.Add("Products");

        ws.Cell(1, 1).Value = "Mã hàng";
        ws.Cell(1, 2).Value = "Tên hàng";
        ws.Cell(1, 3).Value = "Mã vạch";
        ws.Cell(1, 4).Value = "Nhóm hàng";
        ws.Cell(1, 5).Value = "Thương hiệu";
        ws.Cell(1, 6).Value = "Giá vốn";
        ws.Cell(1, 7).Value = "Giá bán";
        ws.Cell(1, 8).Value = "Tồn kho";
        ws.Cell(1, 9).Value = "Tồn tối thiểu";
        ws.Cell(1, 10).Value = "Tồn tối đa";
        ws.Cell(1, 11).Value = "Vị trí";
        ws.Cell(1, 12).Value = "Khối lượng";
        ws.Cell(1, 13).Value = "Đơn vị khối lượng";
        ws.Cell(1, 14).Value = "Mô tả";
        ws.Cell(1, 15).Value = "Bán trực tiếp";
        ws.Cell(1, 16).Value = "Loại hàng";

        ws.Cell(2, 1).Value = "SP001";
        ws.Cell(2, 2).Value = "Ao so mi";
        ws.Cell(2, 3).Value = "893000000001";
        ws.Cell(2, 4).Value = "Thoi trang";
        ws.Cell(2, 5).Value = "Local Brand";
        ws.Cell(2, 6).Value = 150000;
        ws.Cell(2, 7).Value = 250000;
        ws.Cell(2, 8).Value = 20;
        ws.Cell(2, 9).Value = 5;
        ws.Cell(2, 10).Value = 100;
        ws.Cell(2, 11).Value = "Ke A1";
        ws.Cell(2, 12).Value = 500;
        ws.Cell(2, 13).Value = "g";
        ws.Cell(2, 14).Value = "Mau tay dai";
        ws.Cell(2, 15).Value = "Có";
        ws.Cell(2, 16).Value = "goods";

        var header = ws.Range(1, 1, 1, 16);
        header.Style.Font.Bold = true;
        header.Style.Fill.BackgroundColor = XLColor.FromHtml("#E0F2FE");
        ws.Columns().AdjustToContents();

        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }
}
