using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Quay27.Application.Abstractions;
using Quay27.Application.Common.Exceptions;
using System.Security.Authentication;

namespace Quay27.Infrastructure.Storage;

public sealed class R2ObjectStorageClient : IObjectStorageClient
{
    private readonly R2StorageOptions _options;
    private readonly IAmazonS3? _s3Client;
    private readonly string? _configurationError;
    private readonly ILogger<R2ObjectStorageClient> _logger;

    public R2ObjectStorageClient(IOptions<R2StorageOptions> options)
        : this(options, NullLogger<R2ObjectStorageClient>.Instance)
    {
    }

    public R2ObjectStorageClient(
        IOptions<R2StorageOptions> options,
        ILogger<R2ObjectStorageClient> logger)
    {
        _options = options.Value;
        _logger = logger;
        _configurationError = _options.GetConfigurationError();

        if (_configurationError is not null)
        {
            _s3Client = null;
            _logger.LogWarning("R2 storage is not configured: {Error}", _configurationError);
            return;
        }

        var serviceUrl = $"https://{_options.AccountId.Trim()}.r2.cloudflarestorage.com";
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
        if (_s3Client is null || _configurationError is not null)
        {
            throw new UpstreamDependencyException(
                $"Image upload is unavailable because object storage is not configured. {_configurationError}",
                errorCode: "r2_storage_not_configured");
        }

        if (request.ContentLength > _options.MaxFileSizeBytes)
        {
            var maxMb = Math.Max(1, _options.MaxFileSizeBytes / (1024 * 1024));
            throw new AppValidationException(
                $"Uploaded file exceeds the maximum size of {maxMb}MB.");
        }

        var extension = Path.GetExtension(request.FileName);
        var assetId = Guid.NewGuid().ToString("N");
        var objectKey = $"{_options.KeyPrefix.TrimEnd('/')}/{uploadedByUserId:N}/{assetId}{extension}";

        var putRequest = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = objectKey,
            InputStream = request.Content,
            ContentType = request.ContentType,
            AutoCloseStream = false,
            UseChunkEncoding = false,
            DisablePayloadSigning = true
        };

        try
        {
            await _s3Client.PutObjectAsync(putRequest, cancellationToken);
        }
        catch (Exception ex) when (IsTlsHandshakeFailure(ex))
        {
            _logger.LogWarning(
                ex,
                "R2 upload failed due to TLS handshake issue for bucket {BucketName} and key {ObjectKey}.",
                _options.BucketName,
                objectKey);

            throw new UpstreamDependencyException(
                "Image upload failed because secure connection to object storage could not be established.",
                errorCode: "r2_storage_handshake_failure",
                innerException: ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "R2 upload failed for bucket {BucketName} and key {ObjectKey}.",
                _options.BucketName,
                objectKey);

            throw new UpstreamDependencyException(
                "Image upload failed because object storage is currently unavailable.",
                errorCode: "r2_storage_connectivity_failure",
                innerException: ex);
        }

        var baseUrl = _options.PublicBaseUrl.TrimEnd('/');
        var publicUrl = $"{baseUrl}/{objectKey}";

        return new ObjectStorageUploadResult(
            assetId,
            objectKey,
            publicUrl,
            request.ContentType,
            request.ContentLength);
    }

    private static bool IsTlsHandshakeFailure(Exception ex)
    {
        for (Exception? current = ex; current is not null; current = current.InnerException)
        {
            if (current is AuthenticationException)
                return true;

            if (current.Message.Contains("HandshakeFailure", StringComparison.OrdinalIgnoreCase))
                return true;

            if (current.Message.Contains("SSL connection could not be established", StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
