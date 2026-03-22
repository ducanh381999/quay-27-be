namespace Quay27_Be.Options;

public class CorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>Browser origins allowed for SignalR + credentialed requests (e.g. http://localhost:3000).</summary>
    public string[] AllowedOrigins { get; set; } = Array.Empty<string>();
}
