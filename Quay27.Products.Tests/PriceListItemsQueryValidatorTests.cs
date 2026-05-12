using System.Linq;
using Quay27.Application.Products;
using Quay27.Application.Validators;

namespace Quay27.Products.Tests;

public class PriceListItemsQueryValidatorTests
{
    private readonly PriceListItemsQueryValidator _validator = new();

    [Fact]
    public void Should_fail_when_operator_is_set_without_compare_fields()
    {
        var result = _validator.Validate(new PriceListItemsQuery
        {
            PriceListIds = [Guid.NewGuid()],
            PriceOperator = "gt"
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_pass_when_filter_tuple_is_complete()
    {
        var result = _validator.Validate(new PriceListItemsQuery
        {
            PriceListIds = [Guid.NewGuid()],
            PriceOperator = "gt",
            ComparePrice = "lastImportPrice",
            CompareValue = 1000
        });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Should_fail_when_groupIds_exceeds_limit()
    {
        var tooMany = Enumerable.Range(0, 201).Select(_ => Guid.NewGuid()).ToList();
        var result = _validator.Validate(new PriceListItemsQuery
        {
            PriceListIds = [Guid.NewGuid()],
            GroupIds = tooMany,
        });

        Assert.False(result.IsValid);
    }
}
