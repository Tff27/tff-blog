using AwesomeAssertions;
using Tff.Blog.Api.Feeds;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Unit.Tests.Feeds;

public class FeedItemSelectorTests
{
    [Fact]
    public void Select_ExcludesDrafts()
    {
        var posts = new[]
        {
            CreatePost("published", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreatePost("draft", new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc), draft: true),
        };

        var selected = FeedItemSelector.Select(posts);

        selected.Should().ContainSingle().Which.Slug.Should().Be("published");
    }

    [Fact]
    public void Select_ExcludesPostsWithoutASlug()
    {
        var posts = new[]
        {
            CreatePost("published", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreatePost(string.Empty, new DateTime(2026, 2, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreatePost("   ", new DateTime(2026, 3, 1, 0, 0, 0, DateTimeKind.Utc)),
        };

        var selected = FeedItemSelector.Select(posts);

        selected.Should().ContainSingle().Which.Slug.Should().Be("published");
    }

    [Fact]
    public void Select_OrdersPostsByDateDescending()
    {
        var posts = new[]
        {
            CreatePost("oldest", new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreatePost("newest", new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
            CreatePost("middle", new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc)),
        };

        var selected = FeedItemSelector.Select(posts);

        selected.Select(post => post.Slug).Should().ContainInOrder("newest", "middle", "oldest");
    }

    [Fact]
    public void Select_ReturnsTheLatestFifteenPosts()
    {
        var posts = Enumerable
            .Range(1, 20)
            .Select(index => CreatePost($"post-{index}", new DateTime(2026, 1, index, 0, 0, 0, DateTimeKind.Utc)))
            .ToList();

        var selected = FeedItemSelector.Select(posts);

        selected.Should().HaveCount(FeedConstants.ItemLimit);
        selected.First().Slug.Should().Be("post-20");
        selected.Last().Slug.Should().Be("post-6");
    }

    [Fact]
    public void Select_WhenThereAreNoPosts_ReturnsAnEmptyList()
    {
        var selected = FeedItemSelector.Select(Array.Empty<PostModel>());

        selected.Should().BeEmpty();
    }

    internal static PostModel CreatePost(string slug, DateTime date, bool draft = false)
    {
        return new PostModel
        {
            Slug = slug,
            Title = $"Title for {slug}",
            Description = $"Description for {slug}",
            Date = date,
            Draft = draft,
            Tags = new[] { "dotnet" },
        };
    }
}
