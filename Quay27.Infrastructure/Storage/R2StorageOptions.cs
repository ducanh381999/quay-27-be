namespace Quay27.Infrastructure.Storage;

public sealed class R2StorageOptions
{
    public const string SectionName = "R2Storage";
    public const long DefaultMaxFileSizeBytes = 10 * 1024 * 1024;

    public string AccountId { get; set; } = "";
    public string AccessKeyId { get; set; } = "";
    public string SecretAccessKey { get; set; } = "";
    public string BucketName { get; set; } = "";
    public string PublicBaseUrl { get; set; } = "";
    public string KeyPrefix { get; set; } = "products/images";
    public long MaxFileSizeBytes { get; set; } = DefaultMaxFileSizeBytes;

    public bool IsConfigured() =>
        !string.IsNullOrWhiteSpace(AccountId)
        && !string.IsNullOrWhiteSpace(AccessKeyId)
        && !string.IsNullOrWhiteSpace(SecretAccessKey)
        && !string.IsNullOrWhiteSpace(BucketName)
        && !string.IsNullOrWhiteSpace(PublicBaseUrl);

    public string? GetConfigurationError()
    {
        if (string.IsNullOrWhiteSpace(AccountId))
            return "R2Storage:AccountId is required.";
        if (string.IsNullOrWhiteSpace(AccessKeyId))
            return "R2Storage:AccessKeyId is required.";
        if (string.IsNullOrWhiteSpace(SecretAccessKey))
            return "R2Storage:SecretAccessKey is required.";
        if (string.IsNullOrWhiteSpace(BucketName))
            return "R2Storage:BucketName is required.";
        if (string.IsNullOrWhiteSpace(PublicBaseUrl))
            return "R2Storage:PublicBaseUrl is required.";
        return null;
    }
}
