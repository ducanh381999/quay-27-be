using Quay27.Application.Products;

namespace Quay27.Application.Abstractions;

public interface IProductUploadService
{
    Task<UploadedImageAssetResponse> UploadImageAsync(
        Stream content,
        string fileName,
        string contentType,
        long contentLength,
        CancellationToken cancellationToken = default);
}
