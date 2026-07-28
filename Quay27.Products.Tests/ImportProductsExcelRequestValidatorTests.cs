using Quay27.Application.Products;
using Quay27.Application.Validators;

namespace Quay27.Products.Tests;

public class ImportProductsExcelRequestValidatorTests
{
    private readonly ImportProductsExcelRequestValidator _validator = new();

    [Fact]
    public void Should_fail_when_file_is_missing()
    {
        var result = _validator.Validate(new ImportProductsExcelRequest
        {
            FileBytes = Array.Empty<byte>(),
            FileName = "MauFileSanPham.xlsx"
        });

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Should_fail_when_action_is_invalid()
    {
        var result = _validator.Validate(new ImportProductsExcelRequest
        {
            FileBytes = [1, 2, 3],
            FileName = "MauFileSanPham.xlsx",
            DuplicateCodeConflictAction = "keep",
            DuplicateBarcodeConflictAction = ProductImportConflictAction.Error
        });

        Assert.False(result.IsValid);
    }
}
