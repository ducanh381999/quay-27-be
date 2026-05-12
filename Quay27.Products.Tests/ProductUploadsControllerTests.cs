using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Quay27.Application.Abstractions;
using Quay27.Application.Products;

namespace Quay27.Products.Tests;

public class ProductUploadsControllerTests
{
    private static string Quay27BeAssemblyPath =>
        Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory,
            "..", "..", "..", "..", "bin", "Debug", "net8.0", "Quay27-Be.dll"));

    [Fact]
    public void Product_upload_and_product_controllers_require_authorization()
    {
        Assert.True(File.Exists(Quay27BeAssemblyPath), $"Build the API host first so this file exists: {Quay27BeAssemblyPath}");
        var asm = Assembly.LoadFrom(Quay27BeAssemblyPath!);

        var uploadsType = asm.GetType("Quay27_Be.Controllers.ProductUploadsController", throwOnError: true)!;
        var productsType = asm.GetType("Quay27_Be.Controllers.ProductsController", throwOnError: true)!;

        var uploadsAuthorize = Attribute.GetCustomAttribute(uploadsType, typeof(AuthorizeAttribute));
        var productsAuthorize = Attribute.GetCustomAttribute(productsType, typeof(AuthorizeAttribute));

        Assert.NotNull(uploadsAuthorize);
        Assert.NotNull(productsAuthorize);
    }

    [Fact]
    public async Task UploadImage_ReturnsBadRequest_WhenFileIsMissing()
    {
        Assert.True(File.Exists(Quay27BeAssemblyPath), $"Build the API host first so this file exists: {Quay27BeAssemblyPath}");
        var asm = Assembly.LoadFrom(Quay27BeAssemblyPath!);

        var uploadsType = asm.GetType("Quay27_Be.Controllers.ProductUploadsController", throwOnError: true)!;
        var controller = Activator.CreateInstance(uploadsType, new StubProductUploadService())
            ?? throw new InvalidOperationException("Failed to create ProductUploadsController.");

        var method = uploadsType.GetMethod("UploadImage", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new InvalidOperationException("UploadImage not found.");
        var task = (Task)method.Invoke(controller, new object?[] { null, CancellationToken.None })!;
        await task;

        var actionResultT = task.GetType().GetProperty("Result")?.GetValue(task)
            ?? throw new InvalidOperationException("Task has no Result.");
        var inner = actionResultT.GetType().GetProperty("Result")?.GetValue(actionResultT)
            ?? throw new InvalidOperationException("ActionResult has no Result.");
        Assert.IsType<BadRequestObjectResult>(inner);
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
