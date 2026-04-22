using Quay27.Application.Products;
using Quay27.Application.Validators;

namespace Quay27.Products.Tests;

public class UpsertProductRequestValidatorTests
{
    private readonly UpsertProductRequestValidator _validator = new();

    [Fact]
    public void Should_fail_when_min_stock_greater_than_max_stock()
    {
        var request = new UpsertProductRequest
        {
            Name = "Ao khoac",
            ItemType = "goods",
            SalePrice = 100_000,
            CostPrice = 80_000,
            Stock = 10,
            MinStock = 20,
            MaxStock = 5,
            WeightUnit = "g"
        };

        var result = _validator.Validate(request);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.ErrorMessage.Contains("MinStock"));
    }

    [Fact]
    public void Should_pass_for_valid_goods_payload()
    {
        var request = new UpsertProductRequest
        {
            Name = "Ao so mi",
            ItemType = "goods",
            SalePrice = 250_000,
            CostPrice = 150_000,
            Stock = 20,
            MinStock = 5,
            MaxStock = 50,
            WeightValue = 500,
            WeightUnit = "g",
            DirectSale = true
        };

        var result = _validator.Validate(request);

        Assert.True(result.IsValid);
    }
}
