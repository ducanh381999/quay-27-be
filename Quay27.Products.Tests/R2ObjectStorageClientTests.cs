using Microsoft.Extensions.Options;
using Quay27.Application.Abstractions;
using Quay27.Infrastructure.Storage;

namespace Quay27.Products.Tests;

public class R2ObjectStorageClientTests
{
    [Fact]
    public async Task UploadProductImageAsync_Throws_WhenFileExceedsLimit()
    {
        var options = Options.Create(new R2StorageOptions
        {
            AccountId = "acc",
            AccessKeyId = "key",
            SecretAccessKey = "secret",
            BucketName = "bucket",
            PublicBaseUrl = "https://cdn.example.com",
            MaxFileSizeBytes = 1
        });
        var client = new R2ObjectStorageClient(options);
        await using var stream = new MemoryStream(new byte[] { 1, 2 });

        await Assert.ThrowsAsync<InvalidOperationException>(() =>
            client.UploadProductImageAsync(
                new ObjectStorageUploadRequest("demo.png", "image/png", 2, stream),
                Guid.NewGuid(),
                CancellationToken.None));
    }
}
