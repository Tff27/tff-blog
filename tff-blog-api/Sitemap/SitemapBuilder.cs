using System.Xml.Linq;
using Tff.Blog.Api.Feeds;
using Tff.Blog.Api.Services;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Sitemap;

/// <summary>
/// Builds the sitemap: every static page plus every published post. Unlike the feeds it is not
/// capped, because a sitemap should describe the whole site.
/// </summary>
internal static class SitemapBuilder
{
    private static readonly XNamespace SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";

    private static readonly string[] StaticPaths = { "/", "/about", "/cookie-policy" };

    internal static string Build(IEnumerable<PostModel> posts, string siteUrl)
    {
        ArgumentNullException.ThrowIfNull(posts);

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(SitemapNamespace + "urlset",
                StaticPaths.Select(path => CreateStaticUrl(siteUrl, path)),
                SelectPosts(posts).Select(post => CreatePostUrl(siteUrl, post))));

        return FeedFormatting.Serialize(document);
    }

    private static IEnumerable<PostModel> SelectPosts(IEnumerable<PostModel> posts)
    {
        var publishedPosts = posts
            .Where(post => post is not null && !post.Draft && !string.IsNullOrWhiteSpace(post.Slug))
            .ToList();

        return PostSorter.Sort(publishedPosts, "Date", "Descending");
    }

    private static XElement CreateStaticUrl(string siteUrl, string path)
    {
        return new XElement(SitemapNamespace + "url",
            new XElement(SitemapNamespace + "loc", $"{siteUrl}{path}"));
    }

    private static XElement CreatePostUrl(string siteUrl, PostModel post)
    {
        return new XElement(SitemapNamespace + "url",
            new XElement(SitemapNamespace + "loc", FeedUrls.Post(siteUrl, post.Slug!)),
            new XElement(SitemapNamespace + "lastmod", FeedFormatting.Rfc3339(post.Date)));
    }
}
