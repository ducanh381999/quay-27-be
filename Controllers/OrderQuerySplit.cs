namespace Quay27_Be.Controllers;

internal static class OrderQuerySplit
{
    public static IReadOnlyList<string>? SplitStrings(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return null;
        var parts = csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(s => s.Length > 0)
            .ToList();
        return parts.Count == 0 ? null : parts;
    }

    public static IReadOnlyList<Guid>? SplitGuids(string? csv)
    {
        if (string.IsNullOrWhiteSpace(csv)) return null;
        var list = new List<Guid>();
        foreach (var part in csv.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Guid.TryParse(part, out var g)) list.Add(g);
        }

        return list.Count == 0 ? null : list;
    }
}
