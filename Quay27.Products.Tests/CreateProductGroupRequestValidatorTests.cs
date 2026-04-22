using Quay27.Application.Products;
using Quay27.Application.Validators;

namespace Quay27.Products.Tests;

public class CreateProductGroupRequestValidatorTests
{
    private readonly CreateProductGroupRequestValidator _validator = new();

    [Fact]
    public void Should_fail_when_name_is_empty()
    {
        var result = _validator.Validate(new CreateProductGroupRequest
        {
            Name = " "
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_pass_when_name_is_present_and_parent_is_optional()
    {
        var result = _validator.Validate(new CreateProductGroupRequest
        {
            Name = "Nhóm gốc mới",
            ParentId = null
        });

        Assert.True(result.IsValid);
    }
}
