using Quay27.Application.Products;
using Quay27.Application.Validators;

namespace Quay27.Products.Tests;

public class PriceListExportServiceTests
{
    private readonly PriceListItemsQueryValidator _validator = new();

    [Fact]
    public void Should_pass_when_export_query_has_required_price_list_ids()
    {
        var result = _validator.Validate(new PriceListItemsQuery
        {
            PriceListIds = [Guid.NewGuid()]
        });

        Assert.True(result.IsValid);
    }
}
