namespace Tff.Blog.Api.Feeds;

internal static class FeedUrls
{
    internal static string Home(string siteUrl) => $"{siteUrl}/";

    internal static string Feed(string siteUrl, string feedPath) => $"{siteUrl}{feedPath}";

    internal static string Post(string siteUrl, string slug) => $"{siteUrl}/post/{Uri.EscapeDataString(slug)}";
}
