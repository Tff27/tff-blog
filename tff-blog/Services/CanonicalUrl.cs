namespace Tff.Blog.Client.Services;

/// <summary>
/// Normalizes a page URL for the <c>rel="canonical"</c> tag. Query string and fragment are
/// dropped, and so is a trailing slash (except on the site root), so tracking parameters and the
/// <c>/about</c> vs <c>/about/</c> variants do not look like separate pages to search engines.
/// </summary>
public static class CanonicalUrl
{
    public static string From(string url)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        return From(new Uri(url, UriKind.Absolute));
    }

    public static string From(Uri uri)
    {
        ArgumentNullException.ThrowIfNull(uri);

        var builder = new UriBuilder(uri)
        {
            Query = string.Empty,
            Fragment = string.Empty,
        };

        var path = builder.Path.TrimEnd('/');

        builder.Path = path.Length == 0 ? "/" : path;

        return builder.Uri.AbsoluteUri;
    }
}
