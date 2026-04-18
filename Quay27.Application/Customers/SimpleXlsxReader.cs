using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace Quay27.Application.Customers;

internal static class SimpleXlsxReader
{
    internal sealed record ParsedRow(int RowNumber, string InvoiceCode, string TimeRaw, string CustomerRaw, string CreatorRaw, string DraftStaffRaw, string QuantityRaw);

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

        var firstSheetRid = workbookDoc
            .Descendants(mainNs + "sheet")
            .FirstOrDefault()
            ?.Attribute(officeRelNs + "id")
            ?.Value;
        if (string.IsNullOrWhiteSpace(firstSheetRid))
            throw new InvalidOperationException("File Excel không có sheet hợp lệ.");

        var target = workbookRelsDoc
            .Descendants(relNs + "Relationship")
            .FirstOrDefault(r => string.Equals(r.Attribute("Id")?.Value, firstSheetRid, StringComparison.Ordinal))
            ?.Attribute("Target")
            ?.Value;
        if (string.IsNullOrWhiteSpace(target))
            throw new InvalidOperationException("Không tìm thấy sheet đầu tiên trong file Excel.");

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

        const string invoiceCol = "A";
        const string timeCol = "B";
        const string customerCol = "C";
        const string creatorCol = "D";
        const string draftStaffCol = "E";
        const string quantityCol = "F";

        var result = new List<ParsedRow>();
        foreach (var rowNo in cellsByRow.Keys.OrderBy(x => x))
        {
            if (rowNo <= headerRowNumber)
                continue;
            var row = cellsByRow[rowNo];
            var invoice = GetCell(row, invoiceCol);
            var time = GetCell(row, timeCol);
            var customer = GetCell(row, customerCol);
            var creator = GetCell(row, creatorCol);
            var draftStaff = GetCell(row, draftStaffCol);
            var quantity = GetCell(row, quantityCol);
            result.Add(new ParsedRow(rowNo, invoice, time, customer, creator, draftStaff, quantity));
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

    private static void ValidateFixedHeaders(Dictionary<string, string> headerCells)
    {
        var expected = new (string Col, string Header)[]
        {
            ("A", "Mã hóa đơn"),
            ("B", "Thời gian"),
            ("C", "Khách hàng"),
            ("D", "Người tạo"),
            ("E", "NV Soạn"),
            ("F", "Số lượng"),
        };

        foreach (var item in expected)
        {
            var actual = GetCell(headerCells, item.Col);
            if (!string.Equals(NormalizeHeader(actual), NormalizeHeader(item.Header), StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Header không hợp lệ tại cột {item.Col}. Mong đợi '{item.Header}'.");
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
