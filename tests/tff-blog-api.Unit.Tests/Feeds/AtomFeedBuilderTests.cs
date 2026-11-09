using System.Xml.Linq;
using AwesomeAssertions;
using Tff.Blog.Api.Feeds;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Unit.Tests.Feeds;

public class AtomFeedBuilderTests
{
    private const string SiteUrl = "https://www.tiagofonseca.dev";
    private static readonly XNamespace Atom = "http://www.w3.org/2005/Atom";
    private static readonly DateTime PublishedDate = new(2026, 11, 5, 9, 30, 0, DateTimeKind.Utc);

    [Fact]
    public void Build_DeclaresUtf8AndAnAtomFeedRoot()
    {
        var xml = AtomFeedBuilder.Build(Array.Empty<PostModel>(), SiteUrl);

        xml.Should().StartWith("<?xml version=\"1.0\" encoding=\"utf-8\"?>");
        GetFeed(xml).Name.Should().Be(Atom + "feed");
    }

    [Fact]
    public void Build_DescribesTheFeed()
    {
        var feed = GetFeed(AtomFeedBuilder.Build(Array.Empty<PostModel>(), SiteUrl));

        feed.Element(Atom + "title")!.Value.Should().Be(FeedConstants.Title);
        feed.Element(Atom + "subtitle")!.Value.Should().Be(FeedConstants.Description);
        feed.Element(Atom + "id")!.Value.Should().Be($"{SiteUrl}/");
        feed.Element(Atom + "author")!.Element(Atom + "name")!.Value.Should().Be(FeedConstants.Author);
    }

    [Fact]
    public void Build_AddsTheSelfAndAlternateLinks()
    {
        var links = GetFeed(AtomFeedBuilder.Build(Array.Empty<PostModel>(), SiteUrl)).Elements(Atom + "link").ToList();

        links.Should().ContainSingle(link => link.Attribute("rel")!.Value == "self")
            .Which.Attribute("href")!.Value.Should().Be($"{SiteUrl}/atom.xml");
        links.Single(link => link.Attribute("rel")!.Value == "self")
            .Attribute("type")!.Value.Should().Be("application/atom+xml");

        links.Single(link => link.Attribute("rel")!.Value == "alternate")
            .Attribute("href")!.Value.Should().Be($"{SiteUrl}/");
    }

    [Fact]
    public void Build_WithNoPosts_HasNoEntriesAndTheUnixEpochAsUpdated()
    {
        var feed = GetFeed(AtomFeedBuilder.Build(Array.Empty<PostModel>(), SiteUrl));

        feed.Elements(Atom + "entry").Should().BeEmpty();
        feed.Element(Atom + "updated")!.Value.Should().Be("1970-01-01T00:00:00Z");
    }

    [Fact]
    public void Build_AddsOneEntryPerPostNewestFirst()
    {
        var posts = new[]
        {
            FeedItemSelectorTests.CreatePost("older", PublishedDate.AddDays(-10)),
            FeedItemSelectorTests.CreatePost("newer", PublishedDate),
        };

        var entries = GetFeed(AtomFeedBuilder.Build(posts, SiteUrl)).Elements(Atom + "entry").ToList();

        entries.Should().HaveCount(2);
        entries[0].Element(Atom + "title")!.Value.Should().Be("Title for newer");
        entries[1].Element(Atom + "title")!.Value.Should().Be("Title for older");
    }

    [Fact]
    public void Build_DescribesTheEntry()
    {
        var entry = BuildEntries(FeedItemSelectorTests.CreatePost("post", PublishedDate)).Single();

        entry.Element(Atom + "title")!.Value.Should().Be("Title for post");
        entry.Element(Atom + "id")!.Value.Should().Be($"{SiteUrl}/post/post");
        entry.Element(Atom + "published")!.Value.Should().Be("2026-11-05T09:30:00Z");
        entry.Element(Atom + "updated")!.Value.Should().Be("2026-11-05T09:30:00Z");
        entry.Element(Atom + "summary")!.Value.Should().Be("Description for post");
    }

    [Fact]
    public void Build_LinksToThePost()
    {
        var link = BuildEntries(FeedItemSelectorTests.CreatePost("initial-blog", PublishedDate))
            .Single()
            .Element(Atom + "link")!;

        link.Attribute("href")!.Value.Should().Be($"{SiteUrl}/post/initial-blog");
        link.Attribute("rel")!.Value.Should().Be("alternate");
        link.Attribute("type")!.Value.Should().Be("text/html");
    }

    [Fact]
    public void Build_TreatsUnspecifiedDatesAsUtc()
    {
        var unspecified = new DateTime(2026, 11, 5, 9, 30, 0, DateTimeKind.Unspecified);

        var entry = BuildEntries(FeedItemSelectorTests.CreatePost("post", unspecified)).Single();

        entry.Element(Atom + "published")!.Value.Should().Be("2026-11-05T09:30:00Z");
    }

    [Fact]
    public void Build_UsesTheNewestPostAsTheFeedUpdatedDate()
    {
        var posts = new[]
        {
            FeedItemSelectorTests.CreatePost("older", PublishedDate.AddDays(-10)),
            FeedItemSelectorTests.CreatePost("newer", PublishedDate),
        };

        var feed = GetFeed(AtomFeedBuilder.Build(posts, SiteUrl));

        feed.Element(Atom + "updated")!.Value.Should().Be("2026-11-05T09:30:00Z");
    }

    [Fact]
    public void Build_EscapesMarkupInTitlesAndSummaries()
    {
        var post = FeedItemSelectorTests.CreatePost("post", PublishedDate);
        post.Title = "Tom & Jerry <3";
        post.Description = "Use <b>tags</b> & \"quotes\"";

        var entry = BuildEntries(post).Single();

        entry.Element(Atom + "title")!.Value.Should().Be("Tom & Jerry <3");
        entry.Element(Atom + "summary")!.Value.Should().Be("Use <b>tags</b> & \"quotes\"");
    }

    [Fact]
    public void Build_AddsACategoryPerTag()
    {
        var post = FeedItemSelectorTests.CreatePost("post", PublishedDate);
        post.Tags = new[] { "dotnet", "blazor", " " };

        var categories = BuildEntries(post)
            .Single()
            .Elements(Atom + "category")
            .Select(category => category.Attribute("term")!.Value);

        categories.Should().ContainInOrder("dotnet", "blazor");
    }

    [Fact]
    public void Build_LimitsTheFeedToFifteenPosts()
    {
        var posts = Enumerable
            .Range(1, 20)
            .Select(index => FeedItemSelectorTests.CreatePost($"post-{index}", PublishedDate.AddDays(-index)))
            .ToList();

        GetFeed(AtomFeedBuilder.Build(posts, SiteUrl)).Elements(Atom + "entry")
            .Should().HaveCount(FeedConstants.ItemLimit);
    }

    [Fact]
    public void Build_ExcludesDrafts()
    {
        var posts = new[]
        {
            FeedItemSelectorTests.CreatePost("published", PublishedDate),
            FeedItemSelectorTests.CreatePost("draft", PublishedDate.AddDays(1), draft: true),
        };

        var entries = GetFeed(AtomFeedBuilder.Build(posts, SiteUrl)).Elements(Atom + "entry").ToList();

        entries.Should().ContainSingle();
        entries[0].Element(Atom + "id")!.Value.Should().Be($"{SiteUrl}/post/published");
    }

    private static XElement GetFeed(string xml)
    {
        return XDocument.Parse(xml).Root!;
    }

    private static List<XElement> BuildEntries(params PostModel[] posts)
    {
        return GetFeed(AtomFeedBuilder.Build(posts, SiteUrl)).Elements(Atom + "entry").ToList();
    }
}
