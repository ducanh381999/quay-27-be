using System.Text.Json;

namespace Quay27.Application.CustomerGroups;

public static class CustomerGroupRulesJson
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
    };

    public static string SerializeConditions(IReadOnlyList<CustomerGroupConditionDto>? conditions)
    {
        var list = conditions?.ToList() ?? new List<CustomerGroupConditionDto>();
        return JsonSerializer.Serialize(list, Options);
    }

    public static IReadOnlyList<CustomerGroupConditionDto> DeserializeConditions(string? json)
    {
        if (string.IsNullOrWhiteSpace(json) || json == "[]")
            return Array.Empty<CustomerGroupConditionDto>();
        try
        {
            var list = JsonSerializer.Deserialize<CustomerGroupConditionDto[]>(json, Options);
            return list ?? Array.Empty<CustomerGroupConditionDto>();
        }
        catch
        {
            return Array.Empty<CustomerGroupConditionDto>();
        }
    }
}
