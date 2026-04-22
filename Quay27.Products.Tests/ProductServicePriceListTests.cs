namespace Quay27.Products.Tests;

public class ProductServicePriceListTests
{
    [Fact]
    public void Should_accept_price_filter_query_contract_values()
    {
        var query = new Quay27.Application.Products.PriceListItemsQuery
        {
            PriceListIds = [Guid.NewGuid()],
            PriceOperator = "gte",
            ComparePrice = "costPrice",
            CompareValue = 0
        };

        Assert.Equal("gte", query.PriceOperator);
        Assert.Equal("costPrice", query.ComparePrice);
    }

    [Fact]
    public void Should_support_bulk_add_request_flags()
    {
        var request = new Quay27.Application.Products.AddProductsByGroupsRequest
        {
            GroupIds = ["a", "b"],
            IncludeDescendants = true
        };

        Assert.True(request.IncludeDescendants);
        Assert.Equal(2, request.GroupIds.Count);
    }
}
