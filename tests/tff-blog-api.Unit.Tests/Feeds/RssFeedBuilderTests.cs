using System.Xml.Linq;
using AwesomeAssertions;
using Tff.Blog.Api.Feeds;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Unit.Tests.Feeds;

public class RssFeedBuilderTests
{
    private const string SiteUrl = "https://www.tiagofonseca.dev";
    private static readonly XNamespace AtomNamespace = "http://www.w3.org/2005/Atom";
    private static readonly DateTime PublishedDate = new(2026, 11, 5, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Build_DeclaresUtf8AndAnRss20Root()
    {
        var xml = RssFeedBuilder.Build(Array.Empty<PostModel>(), SiteUrl);

        xml.Should().StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>");

        var document = XDocument.Parse(xml);

        document.Root!.Name.LocalName.Should().Be("rss");
        document.Root!.Attribute("version")!.Value.Should().Be("2.0");
    }

    [Fact]
    public void Build_DescribesTheChannel()
    {
        var channel = GetChannel(RssFeedBuilder.Build(Array.Empty<PostModel>(), SiteUrl));

        channel.Element("title")!.Value.Should().Be(FeedConstants.Title);
        channel.Element("link")!.Value.Should().Be($"{SiteUrl}/");
        channel.Element("description")!.Value.Should().Be(FeedConstants.Description);
        channel.Element("language")!.Value.Should().Be(FeedConstants.Language);
    }

    [Fact]
    public void Build_AddsTheAtomSelfLink()
    {
        var channel = GetChannel(RssFeedBuilder.Build(Array.Empty<PostModel>(), SiteUrl));
        var selfLink = channel.Element(AtomNamespace + "link")!;

        selfLink.Attribute("href")!.Value.Should().Be($"{SiteUrl}/rss.xml");
        selfLink.Attribute("rel")!.Value.Should().Be("self");
        selfLink.Attribute("type")!.Value.Should().Be("application/rss+xml");
    }

    [Fact]
    public void Build_WithNoPosts_HasNoItemsAndNoLastBuildDate()
    {
        var channel = GetChannel(RssFeedBuilder.Build(Array.Empty<PostModel>(), SiteUrl));

        channel.Elements("item").Should().BeEmpty();
        channel.Element("lastBuildDate").Should().BeNull();
    }

    [Fact]
    public void Build_AddsOneItemPerPostNewestFirst()
    {
        var posts = new[]
        {
            FeedItemSelectorTests.CreatePost("older", PublishedDate.AddDays(-10)),
            FeedItemSelectorTests.CreatePost("newer", PublishedDate),
        };

        var items = GetChannel(RssFeedBuilder.Build(posts, SiteUrl)).Elements("item").ToList();

        items.Should().HaveCount(2);
        items[0].Element("title")!.Value.Should().Be("Title for newer");
        items[1].Element("title")!.Value.Should().Be("Title for older");
    }

    [Fact]
    public void Build_UsesThePostExcerptAsTheDescription()
    {
        var items = BuildItems(FeedItemSelectorTests.CreatePost("post", PublishedDate));

        items.Single().Element("description")!.Value.Should().Be("Description for post");
    }

    [Fact]
    public void Build_UsesAnAbsolutePermalinkAsTheGuid()
    {
        var item = BuildItems(FeedItemSelectorTests.CreatePost("initial-blog", PublishedDate)).Single();

        item.Element("link")!.Value.Should().Be($"{SiteUrl}/post/initial-blog");
        item.Element("guid")!.Value.Should().Be($"{SiteUrl}/post/initial-blog");
        item.Element("guid")!.Attribute("isPermaLink")!.Value.Should().Be("true");
    }

    [Fact]
    public void Build_FormatsThePubDateAsRfc822()
    {
        var item = BuildItems(FeedItemSelectorTests.CreatePost("post", PublishedDate)).Single();

        item.Element("pubDate")!.Value.Should().Be("Thu, 05 Nov 2026 09:30:00 GMT");
    }

    [Fact]
    public void Build_TreatsUnspecifiedDatesAsUtc()
    {
        var unspecified = new DateTime(2026, 11, 5, 9, 30, 0, DateTimeKind.Unspecified);

        var item = BuildItems(FeedItemSelectorTests.CreatePost("post", unspecified)).Single();

        item.Element("pubDate")!.Value.Should().Be("Thu, 05 Nov 2026 09:30:00 GMT");
    }

    [Fact]
    public void Build_UsesTheNewestPostAsTheLastBuildDate()
    {
        var posts = new[]
        {
            FeedItemSelectorTests.CreatePost("older", PublishedDate.AddDays(-10)),
            FeedItemSelectorTests.CreatePost("newer", PublishedDate),
        };

        var channel = GetChannel(RssFeedBuilder.Build(posts, SiteUrl));

        channel.Element("lastBuildDate")!.Value.Should().Be("Thu, 05 Nov 2026 09:30:00 GMT");
    }

    [Fact]
    public void Build_EscapesMarkupInTitlesAndDescriptions()
    {
        var post = FeedItemSelectorTests.CreatePost("post", PublishedDate);
        post.Title = "Tom & Jerry <3";
        post.Description = "Use <b>tags</b> & \"quotes\"";

        var item = BuildItems(post).Single();

        item.Element("title")!.Value.Should().Be("Tom & Jerry <3");
        item.Element("description")!.Value.Should().Be("Use <b>tags</b> & \"quotes\"");
    }

    [Fact]
    public void Build_AddsACategoryPerTag()
    {
        var post = FeedItemSelectorTests.CreatePost("post", PublishedDate);
        post.Tags = new[] { "dotnet", "blazor", " " };

        var categories = BuildItems(post).Single().Elements("category").Select(category => category.Value);

        categories.Should().ContainInOrder("dotnet", "blazor");
    }

    [Fact]
    public void Build_LimitsTheFeedToFifteenPosts()
    {
        var posts = Enumerable
            .Range(1, 20)
            .Select(index => FeedItemSelectorTests.CreatePost($"post-{index}", PublishedDate.AddDays(-index)))
            .ToList();

        var items = GetChannel(RssFeedBuilder.Build(posts, SiteUrl)).Elements("item");

        items.Should().HaveCount(FeedConstants.ItemLimit);
    }

    [Fact]
    public void Build_ExcludesDrafts()
    {
        var posts = new[]
        {
            FeedItemSelectorTests.CreatePost("published", PublishedDate),
            FeedItemSelectorTests.CreatePost("draft", PublishedDate.AddDays(1), draft: true),
        };

        var items = GetChannel(RssFeedBuilder.Build(posts, SiteUrl)).Elements("item").ToList();

        items.Should().ContainSingle();
        items[0].Element("link")!.Value.Should().Be($"{SiteUrl}/post/published");
    }

    private static XElement GetChannel(string xml)
    {
        return XDocument.Parse(xml).Root!.Element("channel")!;
    }

    private static List<XElement> BuildItems(params PostModel[] posts)
    {
        return GetChannel(RssFeedBuilder.Build(posts, SiteUrl)).Elements("item").ToList();
    }
}
