namespace Quay27.Application.Products;

public sealed class UploadedImageAssetResponse
{
    public string AssetId { get; set; } = "";
    public string ObjectKey { get; set; } = "";
    public string PublicUrl { get; set; } = "";
    public string ContentType { get; set; } = "";
    public long SizeBytes { get; set; }
}
