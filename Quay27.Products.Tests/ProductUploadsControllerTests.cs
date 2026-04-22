using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Products;
using Quay27_Be.Controllers;

namespace Quay27.Products.Tests;

public class ProductUploadsControllerTests
{
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
