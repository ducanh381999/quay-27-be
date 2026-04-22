namespace Quay27.Application.Abstractions;

public sealed record ObjectStorageUploadRequest(
    string FileName,
    string ContentType,
    long ContentLength,
    Stream Content);

public sealed record ObjectStorageUploadResult(
    string AssetId,
    string ObjectKey,
    string PublicUrl,
    string ContentType,
    long SizeBytes);

public interface IObjectStorageClient
{
    Task<ObjectStorageUploadResult> UploadProductImageAsync(
        ObjectStorageUploadRequest request,
        Guid uploadedByUserId,
        CancellationToken cancellationToken = default);
}
