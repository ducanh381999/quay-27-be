using Quay27.Application.CustomerGroups;
using Quay27.Application.Validators;

namespace Quay27.Products.Tests;

public class CreateCustomerGroupRequestValidatorTests
{
    private readonly CreateCustomerGroupRequestValidator _validator = new();

    [Fact]
    public void Should_fail_when_name_is_empty()
    {
        var request = new CreateCustomerGroupRequest
        {
            Name = "",
            Description = "desc",
        };

        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_pass_when_payload_is_valid()
    {
        var request = new CreateCustomerGroupRequest
        {
            Name = "Khach VIP",
            Description = "Nhom khach hang than thiet",
        };

        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }

    [Fact]
    public void Should_fail_when_auto_sync_enabled_with_none_mode()
    {
        var request = new CreateCustomerGroupRequest
        {
            Name = "G1",
            MembershipUpdateMode = "none",
            IsAutoMembershipSync = true,
        };

        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_fail_when_percent_discount_out_of_range()
    {
        var request = new CreateCustomerGroupRequest
        {
            Name = "G1",
            DiscountIsPercent = true,
            DiscountAmount = 150m,
        };

        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_fail_when_condition_field_unknown()
    {
        var request = new CreateCustomerGroupRequest
        {
            Name = "G1",
            Conditions =
            [
                new CustomerGroupConditionDto { Field = "Unknown", Op = ">", Value = "1" },
            ],
        };

        var result = _validator.Validate(request);
        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_pass_when_conditions_valid()
    {
        var request = new CreateCustomerGroupRequest
        {
            Name = "G1",
            Conditions =
            [
                new CustomerGroupConditionDto { Field = "Debt", Op = ">=", Value = "0" },
            ],
        };

        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }
}
