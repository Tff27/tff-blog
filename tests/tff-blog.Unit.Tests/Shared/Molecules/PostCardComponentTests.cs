using AwesomeAssertions;
using Bunit;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Tff.Blog.Client.Shared.Molecules;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Client.Unit.Tests.Shared.Molecules;

public class PostCardComponentTests : BunitContext
{
    [Fact]
    public void PostCard_RendersPostDetails()
    {
        var cut = CreateComponent();

        cut.Find("img").GetAttribute("src").Should().Be("https://example.com/cover.png");
        cut.Find("img").GetAttribute("alt").Should().Be("Test post");
        cut.Find("h1").TextContent.Should().Contain("Test post");
        cut.Find("p").TextContent.Trim().Should().Be("A short description");
    }

    [Fact]
    public void PostCard_RendersOneTagPerPostTag()
    {
        var cut = CreateComponent();

        cut.FindAll(".tag").Select(tag => tag.TextContent)
            .Should().ContainInOrder("dotnet", "blazor");
    }

    [Fact]
    public void PostCard_ClickNavigatesToThePostUrl()
    {
        var cut = CreateComponent();

        cut.Find(".card").Click();

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.Uri.Should().EndWith("post/test-post");
    }

    private IRenderedComponent<PostCardComponent> CreateComponent()
    {
        return Render<PostCardComponent>(parameters => parameters
            .Add(component => component.Post, new PostModel
            {
                Title = "Test post",
                Slug = "test-post",
                Description = "A short description",
                Image = "https://example.com/cover.png",
                Date = new DateTime(2024, 1, 1),
                Tags = new[] { "dotnet", "blazor" },
            }));
    }
}
