using AwesomeAssertions;
using Bunit;
using Tff.Blog.Client.Shared.Molecules;
using Tff.Blog.Client.ViewModels;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Client.Unit.Tests.Shared.Molecules;

public class PostCardComponentTests : BunitContext
{
    [Fact]
    public void PostCard_RendersPostDetails()
    {
        var cut = CreateComponent();

        cut.Find("img").GetAttribute("src").Should().Be("https://example.com/cover.png");
        cut.Find("img").GetAttribute("alt").Should().BeEmpty();
        cut.Find("h2").TextContent.Should().Contain("Test post");
        cut.Find(".post-card__description").TextContent.Trim().Should().Be("A short description");
    }

    [Fact]
    public void PostCard_LinksToThePostUrl()
    {
        var cut = CreateComponent();

        cut.Find("a.post-card").GetAttribute("href").Should().EndWith("post/test-post");
    }

    [Fact]
    public void PostCard_RendersOneTagPerPostTag()
    {
        var cut = CreateComponent();

        cut.FindAll(".tag").Select(tag => tag.TextContent)
            .Should().ContainInOrder("dotnet", "blazor");
    }

    [Fact]
    public void PostCard_FormatsTheDateAndReadTime()
    {
        var cut = CreateComponent();

        cut.Find(".post-card__meta").TextContent.Should().Contain("January 1, 2024");
        cut.Find(".post-card__meta").TextContent.Should().Contain("1 min read");
    }

    [Fact]
    public void PostCard_WhenFeatured_AddsTheFeaturedClass()
    {
        var cut = Render<PostCardComponent>(parameters => parameters
            .Add(component => component.Post, CreatePost())
            .Add(component => component.Featured, true));

        cut.Find("a.post-card").ClassList.Should().Contain("post-card--featured");
    }

    private IRenderedComponent<PostCardComponent> CreateComponent()
    {
        return Render<PostCardComponent>(parameters => parameters
            .Add(component => component.Post, CreatePost()));
    }

    private static PostViewModel CreatePost() => new(new PostModel
    {
        Title = "Test post",
        Slug = "test-post",
        Description = "A short description",
        Image = "https://example.com/cover.png",
        Date = new DateTime(2024, 1, 1),
        ReadTimeInMinutes = 1,
        Tags = new[] { "dotnet", "blazor" },
    });
}
