using System.Globalization;
using System.IO.Compression;
using System.Text;
using System.Xml.Linq;

namespace Quay27.Application.Purchasing;

internal static class ReceiptExcelReader
{
    internal sealed record ParsedRow(
        int RowNumber,
        string ProductCode,
        decimal Quantity,
        decimal UnitPrice,
        decimal Discount);

    public static IReadOnlyList<ParsedRow> ReadRows(byte[] fileBytes)
    {
        using var ms = new MemoryStream(fileBytes, writable: false);
        using var zip = new ZipArchive(ms, ZipArchiveMode.Read, leaveOpen: false);

        var workbookEntry = zip.GetEntry("xl/workbook.xml")
            ?? throw new InvalidOperationException("File Excel không hợp lệ.");
        var workbookRelsEntry = zip.GetEntry("xl/_rels/workbook.xml.rels")
            ?? throw new InvalidOperationException("File Excel không hợp lệ.");

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
            throw new InvalidOperationException("Không tìm thấy sheet đầu tiên.");

        var worksheetPath = target.Replace("\\", "/").TrimStart('/');
        if (!worksheetPath.StartsWith("xl/", StringComparison.OrdinalIgnoreCase))
            worksheetPath = $"xl/{worksheetPath}";

        var worksheetEntry = zip.GetEntry(worksheetPath)
            ?? throw new InvalidOperationException("Không đọc được nội dung worksheet.");

        var sharedStrings = ReadSharedStrings(zip);
        var cellsByRow = ReadCellsByRow(worksheetEntry, sharedStrings);
        if (cellsByRow.Count == 0)
            return [];

        var headerRow = cellsByRow.Keys.Min();
        var result = new List<ParsedRow>();
        foreach (var rowNo in cellsByRow.Keys.OrderBy(x => x))
        {
            if (rowNo <= headerRow) continue;
            var row = cellsByRow[rowNo];
            var productCode = GetCell(row, "A");
            if (string.IsNullOrWhiteSpace(productCode))
                continue;

            var quantity = ParseDecimal(GetCell(row, "E"));
            if (quantity <= 0m)
                quantity = ParseDecimal(GetCell(row, "C"));
            var unitPrice = ParseDecimal(GetCell(row, "F"));
            if (unitPrice <= 0m)
                unitPrice = ParseDecimal(GetCell(row, "D"));
            var discount = ParseDecimal(GetCell(row, "G"));

            result.Add(new ParsedRow(
                rowNo,
                productCode.Trim(),
                quantity <= 0m ? 1m : quantity,
                unitPrice < 0m ? 0m : unitPrice,
                discount < 0m ? 0m : discount));
        }

        return result;
    }

    private static decimal ParseDecimal(string raw)
    {
        var clean = raw.Trim().Replace(",", string.Empty).Replace(" ", string.Empty);
        if (decimal.TryParse(clean, NumberStyles.Any, CultureInfo.InvariantCulture, out var val))
            return val;
        return 0m;
    }

    private static List<string> ReadSharedStrings(ZipArchive zip)
    {
        var entry = zip.GetEntry("xl/sharedStrings.xml");
        if (entry is null) return [];
        XNamespace ns = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        var doc = XDocument.Load(entry.Open());
        return doc.Descendants(ns + "si")
            .Select(si => string.Concat(si.Descendants(ns + "t").Select(t => t.Value)))
            .ToList();
    }

    private static Dictionary<int, Dictionary<string, string>> ReadCellsByRow(
        ZipArchiveEntry worksheetEntry,
        List<string> sharedStrings)
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
                var col = GetColumnLetters(cell.Attribute("r")?.Value);
                if (string.IsNullOrWhiteSpace(col)) continue;
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
        if (string.IsNullOrWhiteSpace(cellRef)) return string.Empty;
        var sb = new StringBuilder();
        foreach (var ch in cellRef)
        {
            if (char.IsLetter(ch)) sb.Append(ch);
            else break;
        }
        return sb.ToString();
    }

    private static string GetCell(Dictionary<string, string> row, string col)
        => row.TryGetValue(col, out var val) ? val.Trim() : string.Empty;
}
