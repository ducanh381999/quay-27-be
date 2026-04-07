using System.Text.Json;

namespace Quay27.Application.Customers;

public record ExportGridColumn(string HeaderName, string Field);

public record ExportGridRequest(
    string SheetName,
    IReadOnlyList<ExportGridColumn> Columns,
    IReadOnlyList<IReadOnlyDictionary<string, JsonElement>> Rows
);
