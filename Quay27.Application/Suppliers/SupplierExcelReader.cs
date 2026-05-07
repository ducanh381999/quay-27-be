using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace Quay27.Application.Suppliers;

/// <summary>
/// Đọc file Excel theo template MauFileNhaCungCap.xlsx (sheet SupplierTemplate).
/// </summary>
internal static class SupplierExcelReader
{
    internal sealed record ParsedRow(
        int RowNumber,
        string Code,
        string Name,
        string Email,
        string Phone,
        string Address,
        string Region,
        string Ward,
        string CurrentDebtRaw,
        string TaxCode,
        string Notes,
        string GroupName,
        string StatusRaw,
        string CompanyName);

    private const string TargetSheetName = "SupplierTemplate";

    public static IReadOnlyList<ParsedRow> ReadMappedRows(byte[] fileBytes)
    {
        using var ms = new MemoryStream(fileBytes, writable: false);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false);

        var workbookEntry = zip.GetEntry("xl/workbook.xml")
            ?? throw new InvalidOperationException("File Excel không hợp lệ (thiếu workbook.xml).");
        var workbookRelsEntry = zip.GetEntry("xl/_rels/workbook.xml.rels")
            ?? throw new InvalidOperationException("File Excel không hợp lệ (thiếu workbook.xml.rels).");

        var workbookDoc = XDocument.Load(workbookEntry.Open());
        var workbookRelsDoc = XDocument.Load(workbookRelsEntry.Open());
        XNamespace relNs = "http://schemas.openxmlformats.org/package/2006/relationships";
        XNamespace mainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        XNamespace officeRelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        // Ưu tiên sheet tên SupplierTemplate; nếu không có, lấy sheet đầu tiên có header hợp lệ.
        var sheetEls = workbookDoc.Descendants(mainNs + "sheet").ToList();
        var targetSheetEl = sheetEls.FirstOrDefault(s =>
                                string.Equals(s.Attribute("name")?.Value, TargetSheetName, StringComparison.OrdinalIgnoreCase))
                            ?? sheetEls.FirstOrDefault();
        if (targetSheetEl is null)
            throw new InvalidOperationException("File Excel không có sheet hợp lệ.");

        var rid = targetSheetEl.Attribute(officeRelNs + "id")?.Value
            ?? throw new InvalidOperationException("Sheet không có rId hợp lệ.");

        var target = workbookRelsDoc
            .Descendants(relNs + "Relationship")
            .FirstOrDefault(r => string.Equals(r.Attribute("Id")?.Value, rid, StringComparison.Ordinal))
            ?.Attribute("Target")
            ?.Value
            ?? throw new InvalidOperationException("Không tìm thấy worksheet target trong workbook.rels.");

        var worksheetPath = target.Replace("\\", "/").TrimStart('/');
        if (!worksheetPath.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
            worksheetPath = $"xl/{worksheetPath}";

        var worksheetEntry = zip.GetEntry(worksheetPath)
            ?? throw new InvalidOperationException("Không đọc được nội dung worksheet.");

        var sharedStrings = ReadSharedStrings(zip);
        var cellsByRow = ReadCellsByRow(worksheetEntry, sharedStrings);
        if (cellsByRow.Count == 0)
            return [];

        const int headerRowNumber = 1;
        if (!cellsByRow.TryGetValue(headerRowNumber, out var headerCells))
            throw new InvalidOperationException("Dòng 1 phải là header hợp lệ.");
        ValidateFixedHeaders(headerCells);

        const string codeCol = "A";
        const string nameCol = "B";
        const string emailCol = "C";
        const string phoneCol = "D";
        const string addressCol = "E";
        const string regionCol = "F";
        const string wardCol = "G";
        // H = Tổng mua (Không Import) — bỏ qua
        const string debtCol = "I";
        const string taxCol = "J";
        const string notesCol = "K";
        const string groupCol = "L";
        const string statusCol = "M";
        // N = Tổng mua trừ trả hàng — derived, không nhập
        const string companyCol = "O";

        var result = new List<ParsedRow>();
        foreach (var rowNo in cellsByRow.Keys.OrderBy(x => x))
        {
            if (rowNo <= headerRowNumber)
                continue;
            var row = cellsByRow[rowNo];
            result.Add(new ParsedRow(
                rowNo,
                Code: GetCell(row, codeCol),
                Name: GetCell(row, nameCol),
                Email: GetCell(row, emailCol),
                Phone: GetCell(row, phoneCol),
                Address: GetCell(row, addressCol),
                Region: GetCell(row, regionCol),
                Ward: GetCell(row, wardCol),
                CurrentDebtRaw: GetCell(row, debtCol),
                TaxCode: GetCell(row, taxCol),
                Notes: GetCell(row, notesCol),
                GroupName: GetCell(row, groupCol),
                StatusRaw: GetCell(row, statusCol),
                CompanyName: GetCell(row, companyCol)));
        }
        return result;
    }

    private static List<string> ReadSharedStrings(ZipArchive zip)
    {
        var entry = zip.GetEntry("xl/sharedStrings.xml");
        if (entry is null)
            return [];
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var doc = XDocument.Load(entry.Open());
        return doc.Descendants(ns + "si")
            .Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value)))
            .ToList();
    }

    private static Dictionary<int, Dictionary<string, string>> ReadCellsByRow(ZipArchiveEntry worksheetEntry, List<string> sharedStrings)
    {
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var doc = XDocument.Load(worksheetEntry.Open());
        var result = new Dictionary<int, Dictionary<string, string>>();

        foreach (var rowElem in doc.Descendants(ns + "row"))
        {
            if (!int.TryParse(rowElem.Attribute("r")?.Value, out var rowNo))
                continue;
            var rowMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var cell in rowElem.Elements(ns + "c"))
            {
                var cellRef = cell.Attribute("r")?.Value;
                var col = GetColumnLetters(cellRef);
                if (string.IsNullOrWhiteSpace(col))
                    continue;
                rowMap[col] = ReadCellText(cell, ns, sharedStrings);
            }
            result[rowNo] = rowMap;
        }

        return result;
    }

    private static string ReadCellText(XElement cell, XNamespace ns, List<string> sharedStrings)
    {
        var type = cell.Attribute("t")?.Value;
        if (string.Equals(type, "inlineStr", StringComparison.OrdinalIgnoreCase))
            return string.Concat(cell.Descendants(ns + "t").Select(t => t.Value)).Trim();
        var raw = cell.Element(ns + "v")?.Value?.Trim() ?? string.Empty;
        if (string.Equals(type, "s", StringComparison.OrdinalIgnoreCase) &&
            int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out var idx) &&
            idx >= 0 && idx < sharedStrings.Count)
        {
            return sharedStrings[idx].Trim();
        }
        return raw;
    }

    private static string GetColumnLetters(string? cellRef)
    {
        if (string.IsNullOrWhiteSpace(cellRef))
            return string.Empty;
        var sb = new StringBuilder();
        foreach (var ch in cellRef)
        {
            if (char.IsLetter(ch))
                sb.Append(ch);
            else
                break;
        }
        return sb.ToString();
    }

    /// <summary>
    /// Validate header (chấp nhận các biến thể phụ; chỉ cần các cột bắt buộc đúng vị trí).
    /// </summary>
    private static void ValidateFixedHeaders(Dictionary<string, string> headerCells)
    {
        var expected = new (string Col, string[] AcceptedHeaders)[]
        {
            ("A", new[] { "Mã nhà cung cấp" }),
            ("B", new[] { "Tên nhà cung cấp" }),
            ("C", new[] { "Email" }),
            ("D", new[] { "Điện thoại" }),
            ("E", new[] { "Địa chỉ" }),
            ("F", new[] { "Khu vực" }),
            ("G", new[] { "Phường/Xã" }),
            ("H", new[] { "Tổng mua (Không Import)", "Tổng mua" }),
            ("I", new[] { "Nợ cần trả hiện tại", "Nợ hiện tại" }),
            ("J", new[] { "Mã số thuế" }),
            ("K", new[] { "Ghi chú" }),
            ("L", new[] { "Nhóm nhà cung cấp" }),
            ("M", new[] { "Trạng thái" }),
            ("N", new[] { "Tổng mua trừ trả hàng" }),
            ("O", new[] { "Công ty" }),
        };

        foreach (var item in expected)
        {
            var actual = NormalizeHeader(GetCell(headerCells, item.Col));
            var ok = item.AcceptedHeaders.Any(h => string.Equals(NormalizeHeader(h), actual, StringComparison.Ordinal));
            if (!ok)
            {
                throw new InvalidOperationException(
                    $"Header không hợp lệ tại cột {item.Col}. Mong đợi '{item.AcceptedHeaders[0]}'.");
            }
        }
    }

    private static string GetCell(Dictionary<string, string> row, string col)
        => row.TryGetValue(col, out var val) ? val.Trim() : string.Empty;

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
