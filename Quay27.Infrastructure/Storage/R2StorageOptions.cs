namespace Quay27.Infrastructure.Storage;

public sealed class R2StorageOptions
{
    public const string SectionName = "R2Storage";

    public string AccountId { get; set; } = "";
    public string AccessKeyId { get; set; } = "";
    public string SecretAccessKey { get; set; } = "";
    public string BucketName { get; set; } = "";
    public string PublicBaseUrl { get; set; } = "";
    public string KeyPrefix { get; set; } = "products/images";
    public long MaxFileSizeBytes { get; set; } = 2 * 1024 * 1024;
}
