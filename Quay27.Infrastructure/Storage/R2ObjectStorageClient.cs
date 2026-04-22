using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Options;
using Quay27.Application.Abstractions;

namespace Quay27.Infrastructure.Storage;

public sealed class R2ObjectStorageClient : IObjectStorageClient
{
    private readonly R2StorageOptions _options;
    private readonly IAmazonS3 _s3Client;

    public R2ObjectStorageClient(IOptions<R2StorageOptions> options)
    {
        _options = options.Value;

        var serviceUrl = $"https://{_options.AccountId}.r2.cloudflarestorage.com";
        var config = new AmazonS3Config
        {
            ServiceURL = serviceUrl,
            ForcePathStyle = true
        };

        _s3Client = new AmazonS3Client(_options.AccessKeyId, _options.SecretAccessKey, config);
    }

    public async Task<ObjectStorageUploadResult> UploadProductImageAsync(
        ObjectStorageUploadRequest request,
        Guid uploadedByUserId,
        CancellationToken cancellationToken = default)
    {
        if (request.ContentLength > _options.MaxFileSizeBytes)
            throw new InvalidOperationException("Uploaded file exceeds configured size limit.");

        var extension = Path.GetExtension(request.FileName);
        var assetId = Guid.NewGuid().ToString("N");
        var objectKey = $"{_options.KeyPrefix.TrimEnd('/')}/{uploadedByUserId:N}/{assetId}{extension}";

        var putRequest = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = request.Content,
            ContentType = request.ContentType,
            AutoCloseStream = false
        };

        await _s3Client.PutObjectAsync(putRequest, cancellationToken);

        var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
        var publicUrl = $"{baseUrl}/{objectKey}";

        return new ObjectStorageUploadResult(
            assetId,
            objectKey,
            publicUrl,
            request.ContentType,
            request.ContentLength);
    }
}
