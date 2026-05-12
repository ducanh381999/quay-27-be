using ClosedXML.Excel;

namespace Quay27.Application.CustomerProfiles;

/// <summary>Generates a minimal .xlsx template whose headers match <see cref="CustomerProfileExcelReader"/>.</summary>
public static class CustomerProfileImportTemplateBuilder
{
    public static byte[] Build()
    {
        using var wb = new XLWorkbook();
        var ws = wb.AddWorksheet("KhachHang");
        var headers = new[]
        {
            "TenKhachHang", "MaKhachHang", "DienThoai1", "DienThoai2", "NgaySinh", "GioiTinh", "Email", "Facebook",
            "DiaChi", "TinhThanh", "PhuongXa", "NhomKhachHang", "GhiChu", "LoaiKhachHang", "TenNguoiMuaHang",
            "MaSoThue", "DiaChiXuatHoaDon", "TinhThanhHD", "PhuongXaHD", "CMND_CCCD", "Passport", "EmailHD",
            "DienThoaiHD", "NganHang", "SoTaiKhoan", "DuNoCuoi",
        };
        for (var i = 0; i < headers.Length; i++)
            ws.Cell(1, i + 1).Value = headers[i];

        ws.SheetView.FreezeRows(1);
        using var ms = new MemoryStream();
        wb.SaveAs(ms);
        return ms.ToArray();
    }
}
