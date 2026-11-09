using System.Xml.Linq;
using AwesomeAssertions;
using Tff.Blog.Api.Sitemap;
using Tff.Blog.Api.Unit.Tests.Feeds;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Unit.Tests.Sitemap;

public class SitemapBuilderTests
{
    private const string SiteUrl = "https://www.tiagofonseca.dev";
    private static readonly XNamespace SitemapNamespace = "http://www.sitemaps.org/schemas/sitemap/0.9";
    private static readonly DateTime PublishedDate = new(2026, 11, 5, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Build_DeclaresUtf8AndAnUrlsetRootInTheSitemapNamespace()
    {
        var xml = SitemapBuilder.Build(Array.Empty<PostModel>(), SiteUrl);

        xml.Should().StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>");

        var document = XDocument.Parse(xml);

        document.Root!.Name.LocalName.Should().Be("urlset");
        document.Root!.Name.Namespace.Should().Be(SitemapNamespace);
    }

    [Fact]
    public void Build_ListsTheStaticPages()
    {
        var locations = BuildLocations();

        locations.Should().ContainInOrder(
            $"{SiteUrl}/",
            $"{SiteUrl}/about",
            $"{SiteUrl}/cookie-policy");
    }

    [Fact]
    public void Build_AddsAnAbsoluteUrlPerPublishedPost()
    {
        var locations = BuildLocations(FeedItemSelectorTests.CreatePost("initial-blog", PublishedDate));

        locations.Should().Contain($"{SiteUrl}/post/initial-blog");
    }

    [Fact]
    public void Build_DoesNotCapTheNumberOfPosts()
    {
        var posts = Enumerable
            .Range(1, 20)
            .Select(index => FeedItemSelectorTests.CreatePost($"post-{index}", PublishedDate.AddDays(-index)))
            .ToList();

        var urls = GetUrls(SitemapBuilder.Build(posts, SiteUrl));

        urls.Should().HaveCount(3 + 20);
    }

    [Fact]
    public void Build_OrdersPostsByDateDescending()
    {
        var posts = new[]
        {
            FeedItemSelectorTests.CreatePost("oldest", PublishedDate.AddDays(-20)),
            FeedItemSelectorTests.CreatePost("newest", PublishedDate),
            FeedItemSelectorTests.CreatePost("middle", PublishedDate.AddDays(-10)),
        };

        var postLocations = GetUrls(SitemapBuilder.Build(posts, SiteUrl))
            .Skip(3)
            .Select(url => url.Element(SitemapNamespace + "loc")!.Value)
            .ToList();

        postLocations.Should().ContainInOrder(
            $"{SiteUrl}/post/newest",
            $"{SiteUrl}/post/middle",
            $"{SiteUrl}/post/oldest");
    }

    [Fact]
    public void Build_ExcludesDraftsAndPostsWithoutASlug()
    {
        var posts = new[]
        {
            FeedItemSelectorTests.CreatePost("published", PublishedDate),
            FeedItemSelectorTests.CreatePost("draft", PublishedDate, draft: true),
            FeedItemSelectorTests.CreatePost(string.Empty, PublishedDate),
            FeedItemSelectorTests.CreatePost("   ", PublishedDate),
        };

        var locations = BuildLocations(posts);

        locations.Should().Contain($"{SiteUrl}/post/published");
        locations.Should().NotContain($"{SiteUrl}/post/draft");
        locations.Should().NotContain(location => location.StartsWith($"{SiteUrl}/post/ ", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_FormatsLastModAsRfc3339()
    {
        var url = GetUrls(SitemapBuilder.Build(new[] { FeedItemSelectorTests.CreatePost("post", PublishedDate) }, SiteUrl)).Last();

        url.Element(SitemapNamespace + "lastmod")!.Value.Should().Be("2026-11-05T09:30:00Z");
    }

    [Fact]
    public void Build_DoesNotAddLastModToTheStaticPages()
    {
        var urls = GetUrls(SitemapBuilder.Build(Array.Empty<PostModel>(), SiteUrl));

        urls.Should().OnlyContain(url => url.Element(SitemapNamespace + "lastmod") == null);
    }

    [Fact]
    public void Build_UrlEncodesSlugs()
    {
        var url = GetUrls(SitemapBuilder.Build(
            new[] { FeedItemSelectorTests.CreatePost("post with spaces", PublishedDate) },
            SiteUrl)).Last();

        url.Element(SitemapNamespace + "loc")!.Value.Should().Be($"{SiteUrl}/post/post%20with%20spaces");
    }

    private static List<string> BuildLocations(params PostModel[] posts)
    {
        return GetUrls(SitemapBuilder.Build(posts, SiteUrl))
            .Select(url => url.Element(SitemapNamespace + "loc")!.Value)
            .ToList();
    }

    private static List<XElement> GetUrls(string xml)
    {
        return XDocument
            .Parse(xml)
            .Root!
            .Elements(SitemapNamespace + "url")
            .ToList();
    }
}
