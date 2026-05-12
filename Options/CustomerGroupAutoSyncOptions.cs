namespace Quay27_Be.Options;

public sealed class CustomerGroupAutoSyncOptions
{
    public const string SectionName = "CustomerGroupAutoSync";

    public bool Enabled { get; set; } = true;

    /// <summary>Minimum 5 minutes between runs.</summary>
    public int IntervalMinutes { get; set; } = 360;
}
