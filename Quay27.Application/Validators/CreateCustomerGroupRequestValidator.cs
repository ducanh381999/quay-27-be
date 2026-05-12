using FluentValidation;
using Quay27.Application.CustomerGroups;

namespace Quay27.Application.Validators;

public static class CustomerGroupConditionFieldNames
{
    public static readonly HashSet<string> Allowed = new(StringComparer.OrdinalIgnoreCase)
    {
        "TotalRevenue",
        "TotalInvoiced",
        "RewardPoint",
        "TotalPoint",
        "PurchaseDate",
        "PurchaseNumber",
        "Debt",
        "BirthDay",
        "Age",
        "Gender",
        "Location",
        "Type",
    };
}

public class CreateCustomerGroupRequestValidator : AbstractValidator<CreateCustomerGroupRequest>
{
    public CreateCustomerGroupRequestValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(128);
        RuleFor(x => x.Description).MaximumLength(512);
        RuleFor(x => x.MembershipUpdateMode)
            .Must(m => string.IsNullOrWhiteSpace(m) || IsAllowedMode(m))
            .WithMessage("MembershipUpdateMode must be add, replace, or none.");
        RuleFor(x => x.IsAutoMembershipSync)
            .Equal(false)
            .When(x => string.Equals(NormalizeMode(x.MembershipUpdateMode), "none", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Auto sync cannot be enabled when membership update mode is none.");

        RuleFor(x => x.DiscountAmount)
            .InclusiveBetween(0m, 100m)
            .When(x => x.DiscountIsPercent && x.DiscountAmount.HasValue)
            .WithMessage("Percent discount must be between 0 and 100.");

        RuleFor(x => x.DiscountAmount)
            .InclusiveBetween(0m, 999_999_999_999.99m)
            .When(x => !x.DiscountIsPercent && x.DiscountAmount.HasValue)
            .WithMessage("Discount amount is out of range.");

        RuleFor(x => x.Conditions)
            .Must(c => c == null || c.Count <= 30)
            .WithMessage("At most 30 conditions are allowed.");

        When(x => x.Conditions is { Count: > 0 }, () =>
        {
            RuleForEach(x => x.Conditions!).ChildRules(c =>
            {
                c.RuleFor(z => z.Field).NotEmpty().MaximumLength(64)
                    .Must(f => CustomerGroupConditionFieldNames.Allowed.Contains(f.Trim()))
                    .WithMessage("Unknown condition field.");
                c.RuleFor(z => z.Op).NotEmpty().MaximumLength(8);
                c.RuleFor(z => z.Value).MaximumLength(512);
                c.RuleFor(z => z.Value).NotEmpty().When(z => !IsRadioField(z.Field));
            });
        });
    }

    private static bool IsRadioField(string? field)
    {
        var f = (field ?? "").Trim();
        return f is "Gender" or "Type";
    }

    private static bool IsAllowedMode(string? m)
    {
        var x = (m ?? "").Trim().ToLowerInvariant();
        return x is "add" or "replace" or "none";
    }

    private static string NormalizeMode(string? m) =>
        string.IsNullOrWhiteSpace(m) ? "none" : m.Trim().ToLowerInvariant();
}
