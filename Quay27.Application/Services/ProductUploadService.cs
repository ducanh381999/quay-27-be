using Quay27.Application.Abstractions;
using Quay27.Application.Common.Exceptions;
using Quay27.Application.Products;

namespace Quay27.Application.Services;

public sealed class ProductUploadService : IProductUploadService
{
    private static readonly HashSet<string> AllowedContentTypes =
    [
        "image/jpeg",
        "image/png",
        "image/webp",
        "image/gif"
    ];

    private readonly IObjectStorageClient _storageClient;
    private readonly ICurrentUser _currentUser;

    public ProductUploadService(IObjectStorageClient storageClient, ICurrentUser currentUser)
    {
        _storageClient = storageClient;
        _currentUser = currentUser;
    }

    public async Task<UploadedImageAssetResponse> UploadImageAsync(
        Stream content,
        string fileName,
        string contentType,
        long contentLength,
        CancellationToken cancellationToken = default)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.UserId is null)
            throw new ForbiddenException("Authentication required.");

        if (contentLength <= 0)
            throw new AppValidationException("Uploaded file is empty.");

        if (!AllowedContentTypes.Contains(contentType))
            throw new AppValidationException("Unsupported image content type.");

        var uploadResult = await _storageClient.UploadProductImageAsync(
            new ObjectStorageUploadRequest(fileName, contentType, contentLength, content),
            _currentUser.UserId.Value,
            cancellationToken);

        return new UploadedImageAssetResponse
        {
            AssetId = uploadResult.AssetId,
            ObjectKey = uploadResult.ObjectKey,
            PublicUrl = uploadResult.PublicUrl,
            ContentType = uploadResult.ContentType,
            SizeBytes = uploadResult.SizeBytes
        };
    }
}
