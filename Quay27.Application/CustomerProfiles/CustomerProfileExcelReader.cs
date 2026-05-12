using System.Globalization;
using System.Text;
using ClosedXML.Excel;
using Quay27.Application.Common.Exceptions;

namespace Quay27.Application.CustomerProfiles;

internal static class CustomerProfileExcelReader
{
    internal sealed record ParsedRow(int RowNumber, string? CustomerCode, CreateCustomerProfileRequest Request,
        decimal? ClosingDebtFromFile);

    public static (IXLWorksheet Worksheet, Dictionary<string, int> HeaderMap) ResolveWorksheetAndHeaders(
        XLWorkbook workbook)
    {
        foreach (var ws in workbook.Worksheets)
        {
            var map = BuildHeaderMap(ws);
            if (map.ContainsKey("customername"))
                return (ws, map);
        }

        throw new ConflictException(
            "Không tìm thấy sheet hợp lệ. Cần ít nhất một sheet có cột \"Tên khách hàng\" (hoặc customerName).");
    }

    public static IReadOnlyList<ParsedRow> ReadRows(IXLWorksheet worksheet, Dictionary<string, int> headerMap)
    {
        var list = new List<ParsedRow>();
        foreach (var row in worksheet.RowsUsed().Skip(1))
        {
            var rowNo = row.RowNumber();
            var name = Cell(row, headerMap, "customername");
            if (string.IsNullOrWhiteSpace(name) &&
                string.IsNullOrWhiteSpace(Cell(row, headerMap, "phone1")) &&
                string.IsNullOrWhiteSpace(Cell(row, headerMap, "customercode")))
            {
                continue;
            }

            var req = new CreateCustomerProfileRequest
            {
                CustomerName = name,
                Phone1 = Cell(row, headerMap, "phone1"),
                Phone2 = Cell(row, headerMap, "phone2"),
                Birthday = ParseDateOnly(row, headerMap, "birthday"),
                Gender = Cell(row, headerMap, "gender"),
                Email = Cell(row, headerMap, "email"),
                Facebook = Cell(row, headerMap, "facebook"),
                Address = Cell(row, headerMap, "address"),
                ProvinceCity = Cell(row, headerMap, "provincecity"),
                Ward = Cell(row, headerMap, "ward"),
                CustomerGroup = Cell(row, headerMap, "customergroup"),
                Note = Cell(row, headerMap, "note"),
                BuyerType = NormalizeBuyerType(Cell(row, headerMap, "buyertype")),
                BuyerName = Cell(row, headerMap, "buyername"),
                TaxCode = Cell(row, headerMap, "taxcode"),
                InvoiceAddress = Cell(row, headerMap, "invoiceaddress"),
                InvoiceProvinceCity = Cell(row, headerMap, "invoiceprovincecity"),
                InvoiceWard = Cell(row, headerMap, "invoiceward"),
                IdentityNumber = Cell(row, headerMap, "identitynumber"),
                PassportNumber = Cell(row, headerMap, "passportnumber"),
                InvoiceEmail = Cell(row, headerMap, "invoiceemail"),
                InvoicePhone = Cell(row, headerMap, "invoicephone"),
                BankName = Cell(row, headerMap, "bankname"),
                BankAccountNumber = Cell(row, headerMap, "bankaccountnumber"),
            };

            var codeRaw = Cell(row, headerMap, "customercode");
            var customerCode = string.IsNullOrWhiteSpace(codeRaw) ? null : codeRaw.Trim();
            var closingDebt = ParseDecimalCell(row, headerMap, "closingdebt");

            list.Add(new ParsedRow(rowNo, customerCode, req, closingDebt));
        }

        return list;
    }

    private static string NormalizeBuyerType(string raw)
    {
        var t = raw.Trim();
        if (string.IsNullOrEmpty(t))
            return "individual";
        var c = CompactHeader(t);
        if (c is "company" or "congty" or "cty" or "doanhnghiep" or "dn")
            return "company";
        return "individual";
    }

    private static DateOnly? ParseDateOnly(IXLRow row, IReadOnlyDictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var col))
            return null;
        var cell = row.Cell(col);
        if (cell.IsEmpty())
            return null;

        if (cell.DataType == XLDataType.DateTime && cell.TryGetValue<DateTime>(out var dt))
            return DateOnly.FromDateTime(dt.Date);

        var s = cell.GetString().Trim();
        if (DateOnly.TryParse(s, CultureInfo.InvariantCulture, DateTimeStyles.None, out var d))
            return d;
        if (DateTime.TryParse(s, CultureInfo.GetCultureInfo("vi-VN"), DateTimeStyles.None, out var dt2))
            return DateOnly.FromDateTime(dt2.Date);
        return null;
    }

    private static string Cell(IXLRow row, IReadOnlyDictionary<string, int> map, string key) =>
        map.TryGetValue(key, out var col) ? row.Cell(col).GetString().Trim() : string.Empty;

    private static decimal? ParseDecimalCell(IXLRow row, IReadOnlyDictionary<string, int> map, string key)
    {
        if (!map.TryGetValue(key, out var col))
            return null;
        var cell = row.Cell(col);
        if (cell.IsEmpty())
            return null;
        if (cell.DataType == XLDataType.Number && cell.TryGetValue<decimal>(out var d))
            return d;
        var s = cell.GetString().Trim();
        if (s.Length == 0)
            return null;
        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.GetCultureInfo("vi-VN"), out var vi))
            return vi;
        if (decimal.TryParse(s, NumberStyles.Number, CultureInfo.InvariantCulture, out var inv))
            return inv;
        return null;
    }

    private static Dictionary<string, int> BuildHeaderMap(IXLWorksheet worksheet)
    {
        var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        var row1 = worksheet.Row(1);
        if (row1.IsEmpty())
            return map;

        foreach (var cell in row1.CellsUsed())
        {
            var col = cell.Address.ColumnNumber;
            if (col <= 0)
                continue;
            var compact = CompactHeader(cell.GetString());
            if (string.IsNullOrEmpty(compact))
                continue;
            MapCanonical(compact, col, map);
        }

        return map;
    }

    private static void MapCanonical(string compact, int col, Dictionary<string, int> map)
    {
        void Set(string canonical)
        {
            if (!map.ContainsKey(canonical))
                map[canonical] = col;
        }

        switch (compact)
        {
            case "tenkhachhang":
            case "hoten":
            case "customername":
            case "tenkh":
                Set("customername");
                return;
            case "makhachhang":
            case "makh":
            case "customercode":
            case "ma":
                Set("customercode");
                return;
            case "dienthoai1":
            case "dienthoai":
            case "sodienthoai":
            case "sdt":
            case "phone1":
            case "mobile":
                Set("phone1");
                return;
            case "dienthoai2":
            case "phone2":
                Set("phone2");
                return;
            case "sinhnhat":
            case "ngaysinh":
            case "birthday":
                Set("birthday");
                return;
            case "gioitinh":
            case "gender":
                Set("gender");
                return;
            case "email":
                Set("email");
                return;
            case "facebook":
                Set("facebook");
                return;
            case "diachi":
            case "address":
                Set("address");
                return;
            case "tinhthanh":
            case "tinhthanhpho":
            case "thanhpho":
            case "provincecity":
                Set("provincecity");
                return;
            case "phuongxa":
            case "phuong":
            case "xa":
            case "ward":
                Set("ward");
                return;
            case "nhomkhachhang":
            case "nhom":
            case "customergroup":
                Set("customergroup");
                return;
            case "ghichu":
            case "note":
                Set("note");
                return;
            case "loaikhach":
            case "loaikhachhang":
            case "buyertype":
                Set("buyertype");
                return;
            case "tennguoimuahang":
            case "buyername":
                Set("buyername");
                return;
            case "masothue":
            case "mst":
            case "taxcode":
                Set("taxcode");
                return;
            case "diachixuathoadon":
            case "diachihoadon":
            case "invoiceaddress":
                Set("invoiceaddress");
                return;
            case "tinhthanhxuathoadon":
            case "tinhthanhhd":
            case "invoiceprovincecity":
                Set("invoiceprovincecity");
                return;
            case "phuongxaxuathoadon":
            case "phuongxahd":
            case "invoiceward":
                Set("invoiceward");
                return;
            case "cmnd":
            case "cccd":
            case "socmnd":
            case "cmndcccd":
            case "identitynumber":
                Set("identitynumber");
                return;
            case "passport":
            case "hochieu":
            case "passportnumber":
                Set("passportnumber");
                return;
            case "emailxuathoadon":
            case "emailhoadon":
            case "invoiceemail":
                Set("invoiceemail");
                return;
            case "dienthoaixuathoadon":
            case "dienthoaihd":
            case "invoicephone":
                Set("invoicephone");
                return;
            case "nganhang":
            case "bankname":
                Set("bankname");
                return;
            case "sotaikhoan":
            case "stk":
            case "bankaccountnumber":
                Set("bankaccountnumber");
                return;
            case "dunocuoi":
            case "duno":
            case "congnohientai":
            case "manualcurrentdebt":
            case "closingdebt":
                Set("closingdebt");
                return;
        }
    }

    private static string CompactHeader(string raw)
    {
        var n = NormalizeHeader(raw);
        return n.Replace(" ", string.Empty).Replace("_", string.Empty);
    }

    private static string NormalizeHeader(string value)
    {
        var normalized = value.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var sb = new StringBuilder(normalized.Length);
        foreach (var ch in normalized)
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(ch);
            if (category == UnicodeCategory.NonSpacingMark)
                continue;
            if (char.IsLetterOrDigit(ch))
                sb.Append(ch);
            else
                sb.Append(' ');
        }

        return string.Join(' ', sb.ToString().Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }
}
