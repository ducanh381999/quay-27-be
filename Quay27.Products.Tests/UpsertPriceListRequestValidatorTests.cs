using Quay27.Application.Products;
using Quay27.Application.Validators;

namespace Quay27.Products.Tests;

public class UpsertPriceListRequestValidatorTests
{
    private readonly UpsertPriceListRequestValidator _validator = new();

    [Fact]
    public void Should_fail_when_end_time_before_start_time()
    {
        var request = new UpsertPriceListRequest
        {
            Name = "Bang gia 1",
            Status = "active",
            StartAt = DateTimeOffset.UtcNow,
            EndAt = DateTimeOffset.UtcNow.AddDays(-1),
            Formula = new PriceListFormulaDto
            {
                Source = "costPrice",
                Operation = "add",
                Value = 0,
                Unit = "vnd",
                RoundEnabled = true,
                RoundTo = 1000
            },
            SalesRuleMode = "allow"
        };

        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_fail_when_round_to_is_invalid()
    {
        var request = new UpsertPriceListRequest
        {
            Name = "Bang gia 2",
            Status = "active",
            StartAt = DateTimeOffset.UtcNow,
            EndAt = DateTimeOffset.UtcNow.AddDays(1),
            Formula = new PriceListFormulaDto
            {
                Source = "costPrice",
                Operation = "add",
                Value = 0,
                Unit = "vnd",
                RoundEnabled = true,
                RoundTo = 500
            },
            SalesRuleMode = "allow"
        };

        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_pass_for_valid_payload()
    {
        var request = new UpsertPriceListRequest
        {
            Name = "Bang gia hop le",
            Status = "active",
            StartAt = DateTimeOffset.UtcNow,
            EndAt = DateTimeOffset.UtcNow.AddDays(1),
            Formula = new PriceListFormulaDto
            {
                Source = "costPrice",
                Operation = "add",
                Value = 10,
                Unit = "percent",
                RoundEnabled = true,
                RoundTo = 1000
            },
            SalesRuleMode = "warn",
            Scope = new PriceListScopeDto
            {
                ApplyAllBranches = false,
                BranchIds = ["cn1"],
                ApplyAllCustomerGroups = true,
                CustomerGroupIds = [],
                ApplyAllCashiers = true,
                CashierIds = []
            }
        };

        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }
}
