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
            Description = "desc"
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
            Description = "Nhom khach hang than thiet"
        };

        var result = _validator.Validate(request);
        Assert.True(result.IsValid);
    }
}
