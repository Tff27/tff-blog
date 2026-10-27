using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tff.Blog.Client.Services;
using Tff.Blog.Client.Shared.Organisms;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Client.Unit.Tests.Shared.Organisms;

public class PostListComponentTests : BunitContext
{
    private readonly IBlogContentService _contentService = Substitute.For<IBlogContentService>();

    public PostListComponentTests()
    {
        Services.AddSingleton(_contentService);
    }

    [Fact]
    public void PostList_RendersEachPost()
    {
        _contentService.GetPostsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(BlogContentResult<IReadOnlyList<PostModel>>.Success(
                [CreatePost("First post", "first-post"), CreatePost("Second post", "second-post")])));

        var cut = Render<PostListComponent>();

        cut.FindAll(".card").Should().HaveCount(2);
        cut.Markup.Should().Contain("First post").And.Contain("Second post");
    }

    [Fact]
    public void PostList_WhenLoadingFails_ShowsRetryableError()
    {
        _contentService.GetPostsAsync(Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(BlogContentResult<IReadOnlyList<PostModel>>.Failure("Content unavailable")));

        var cut = Render<PostListComponent>();

        cut.Find(".content-error").TextContent.Should().Contain("Content unavailable");
        cut.FindAll(".retry-button").Should().ContainSingle();
        cut.FindAll(".card").Should().BeEmpty();
    }

    [Fact]
    public void PostList_RetryReloadsThePosts()
    {
        var callCount = 0;
        _contentService.GetPostsAsync(Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(callCount++ == 0
                ? BlogContentResult<IReadOnlyList<PostModel>>.Failure("Content unavailable")
                : BlogContentResult<IReadOnlyList<PostModel>>.Success([CreatePost("Recovered post", "recovered-post")])));

        var cut = Render<PostListComponent>();
        cut.Find(".retry-button").Click();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".content-error").Should().BeEmpty();
            cut.FindAll(".card").Should().ContainSingle();
        });
    }

    private static PostModel CreatePost(string title, string slug) => new()
    {
        Title = title,
        Slug = slug,
        Description = "A description",
        Image = "https://example.com/cover.png",
        Date = new DateTime(2024, 1, 1),
        Tags = ["dotnet"],
        Text = "Body",
    };
}
