using Quay27.Application.Products;
using Quay27.Application.Validators;

namespace Quay27.Products.Tests;

public class PriceListImportServiceTests
{
    private readonly PriceListImportRequestValidator _validator = new();

    [Fact]
    public void Should_fail_when_file_is_missing()
    {
        var result = _validator.Validate(new PriceListImportRequest
        {
            FileBytes = Array.Empty<byte>(),
            FileName = "MauFileBangGia.xlsx"
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_fail_when_file_extension_is_not_xlsx()
    {
        var result = _validator.Validate(new PriceListImportRequest
        {
            FileBytes = [1, 2, 3],
            FileName = "invalid.csv"
        });

        Assert.False(result.IsValid);
    }
}
