using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Products;
using Quay27_Be.Controllers;

namespace Quay27.Products.Tests;

public class ProductUploadsControllerTests
{
    [Fact]
    public void Product_upload_and_product_controllers_require_authorization()
    {
        var uploadsAuthorize = Attribute.GetCustomAttribute(
            typeof(ProductUploadsController),
            typeof(AuthorizeAttribute));
        var productsAuthorize = Attribute.GetCustomAttribute(
            typeof(ProductsController),
            typeof(AuthorizeAttribute));

        Assert.NotNull(uploadsAuthorize);
        Assert.NotNull(productsAuthorize);
    }

    [Fact]
    public async Task UploadImage_ReturnsBadRequest_WhenFileIsMissing()
    {
        var controller = new ProductUploadsController(new StubProductUploadService());

        var result = await controller.UploadImage(null, CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    private sealed class StubProductUploadService : IProductUploadService
    {
        public Task<UploadedImageAssetResponse> UploadImageAsync(
            Stream content,
            string fileName,
            string contentType,
            long contentLength,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new UploadedImageAssetResponse
            {
                AssetId = "a",
                ObjectKey = "k",
                PublicUrl = "u",
                ContentType = contentType,
                SizeBytes = contentLength
            });
        }
    }
}
