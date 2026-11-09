#nullable enable annotations

using System.Xml.Linq;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Feeds;

/// <summary>
/// Builds an RSS 2.0 document. Items carry the post excerpt, not the whole post body.
/// </summary>
internal static class RssFeedBuilder
{
    private static readonly XNamespace AtomNamespace = "http://www.w3.org/2005/Atom";

    internal static string Build(IEnumerable<PostModel> posts, string siteUrl)
    {
        var items = FeedItemSelector.Select(posts);

        var channel = new XElement("channel",
            new XElement("title", FeedConstants.Title),
            new XElement("link", FeedUrls.Home(siteUrl)),
            new XElement("description", FeedConstants.Description),
            new XElement("language", FeedConstants.Language),
            CreateLastBuildDate(items),
            new XElement(AtomNamespace + "link",
                new XAttribute("href", FeedUrls.Feed(siteUrl, FeedConstants.RssPath)),
                new XAttribute("rel", "self"),
                new XAttribute("type", "application/rss+xml")),
            items.Select(post => CreateItem(post, siteUrl)));

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("rss",
                new XAttribute("version", "2.0"),
                new XAttribute(XNamespace.Xmlns + "atom", AtomNamespace),
                channel));

        return FeedFormatting.Serialize(document);
    }

    private static XElement? CreateLastBuildDate(IReadOnlyList<PostModel> items)
    {
        // Derived from the newest post, not from the current time, so the payload (and therefore
        // its ETag) stays stable between requests.
        return items.Count == 0
            ? null
            : new XElement("lastBuildDate", FeedFormatting.Rfc822(items.Max(post => post.Date)));
    }

    private static XElement CreateItem(PostModel post, string siteUrl)
    {
        var link = FeedUrls.Post(siteUrl, post.Slug ?? string.Empty);

        var item = new XElement("item",
            new XElement("title", post.Title ?? string.Empty),
            new XElement("link", link),
            new XElement("guid", new XAttribute("isPermaLink", "true"), link),
            new XElement("pubDate", FeedFormatting.Rfc822(post.Date)),
            new XElement("description", post.Description ?? string.Empty));

        foreach (var tag in post.Tags ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                item.Add(new XElement("category", tag));
            }
        }

        return item;
    }
}
