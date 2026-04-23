namespace Quay27.Application.Products;

public static class ProductMappings
{
    public static string? NormalizeDisplayImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return null;

        var trimmed = imageUrl.Trim();
        if (trimmed.Length == 0)
            return null;

        if (!Uri.TryCreate(trimmed, UriKind.Absolute, out var uri))
            return null;

        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        return uri.ToString();
    }
}
