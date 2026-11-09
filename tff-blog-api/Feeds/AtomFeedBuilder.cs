using System.Xml.Linq;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Feeds;

/// <summary>
/// Builds an Atom 1.0 document. Entries carry the post excerpt, not the whole post body.
/// </summary>
internal static class AtomFeedBuilder
{
    private static readonly XNamespace AtomNamespace = "http://www.w3.org/2005/Atom";

    internal static string Build(IEnumerable<PostModel> posts, string siteUrl)
    {
        var items = FeedItemSelector.Select(posts);

        var document = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement(AtomNamespace + "feed",
                new XElement(AtomNamespace + "title", FeedConstants.Title),
                new XElement(AtomNamespace + "id", FeedUrls.Home(siteUrl)),
                new XElement(AtomNamespace + "subtitle", FeedConstants.Description),
                new XElement(AtomNamespace + "updated", GetUpdated(items)),
                new XElement(AtomNamespace + "link",
                    new XAttribute("href", FeedUrls.Feed(siteUrl, FeedConstants.AtomPath)),
                    new XAttribute("rel", "self"),
                    new XAttribute("type", "application/atom+xml")),
                new XElement(AtomNamespace + "link",
                    new XAttribute("href", FeedUrls.Home(siteUrl)),
                    new XAttribute("rel", "alternate"),
                    new XAttribute("type", "text/html")),
                new XElement(AtomNamespace + "author",
                    new XElement(AtomNamespace + "name", FeedConstants.Author)),
                items.Select(post => CreateEntry(post, siteUrl))));

        return FeedFormatting.Serialize(document);
    }

    private static string GetUpdated(IReadOnlyList<PostModel> items)
    {
        // Atom requires <updated>, and deriving it from the newest post keeps the payload stable
        // between requests (and so keeps the ETag meaningful).
        var updated = items.Count == 0 ? DateTime.UnixEpoch : items.Max(post => post.Date);

        return FeedFormatting.Rfc3339(updated);
    }

    private static XElement CreateEntry(PostModel post, string siteUrl)
    {
        var link = FeedUrls.Post(siteUrl, post.Slug ?? string.Empty);
        var published = FeedFormatting.Rfc3339(post.Date);

        var entry = new XElement(AtomNamespace + "entry",
            new XElement(AtomNamespace + "title", post.Title ?? string.Empty),
            new XElement(AtomNamespace + "id", link),
            new XElement(AtomNamespace + "link",
                new XAttribute("href", link),
                new XAttribute("rel", "alternate"),
                new XAttribute("type", "text/html")),
            new XElement(AtomNamespace + "published", published),
            new XElement(AtomNamespace + "updated", published),
            new XElement(AtomNamespace + "summary", post.Description ?? string.Empty));

        foreach (var tag in post.Tags ?? Array.Empty<string>())
        {
            if (!string.IsNullOrWhiteSpace(tag))
            {
                entry.Add(new XElement(AtomNamespace + "category", new XAttribute("term", tag)));
            }
        }

        return entry;
    }
}
