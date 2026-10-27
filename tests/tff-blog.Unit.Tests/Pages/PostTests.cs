using AwesomeAssertions;
using Bunit;
using Ganss.Xss;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tff.Blog.Client.Pages;
using Tff.Blog.Client.Services;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Client.Unit.Tests.Pages;

public class PostTests : BunitContext
{
    private readonly IBlogContentService _contentService = Substitute.For<IBlogContentService>();

    public PostTests()
    {
        Services.AddSingleton(_contentService);

        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedAttributes.Add("class");
        Services.AddSingleton<IHtmlSanitizer>(sanitizer);
    }

    [Fact]
    public void Post_WhenPostExists_RendersItsContent()
    {
        _contentService.GetPostAsync("test-post", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(BlogContentResult<PostModel>.Success(CreatePost())));

        var cut = Render<Post>(parameters => parameters.Add(component => component.slug, "test-post"));

        cut.Find("article h1").TextContent.Should().Contain("Test post");
        cut.Markup.Should().Contain("<strong>Body</strong>");
        cut.FindAll(".content-error").Should().BeEmpty();
    }

    [Fact]
    public void Post_WhenPostIsMissing_NavigatesTo404()
    {
        _contentService.GetPostAsync("missing-post", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(BlogContentResult<PostModel>.NotFound()));

        Render<Post>(parameters => parameters.Add(component => component.slug, "missing-post"));

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.Uri.Should().EndWith("404");
    }

    [Fact]
    public void Post_WhenLoadingFails_ShowsRetryableErrorWithoutNavigatingAway()
    {
        _contentService.GetPostAsync("test-post", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(BlogContentResult<PostModel>.Failure("Content unavailable")));

        var cut = Render<Post>(parameters => parameters.Add(component => component.slug, "test-post"));

        cut.Find(".content-error").TextContent.Should().Contain("Content unavailable");
        cut.FindAll(".retry-button").Should().ContainSingle();
        cut.FindAll("article").Should().BeEmpty();

        var navigationManager = Services.GetRequiredService<NavigationManager>();
        navigationManager.Uri.Should().NotEndWith("404");
    }

    [Fact]
    public void Post_RetryReloadsThePost()
    {
        var callCount = 0;
        _contentService.GetPostAsync("test-post", Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromResult(callCount++ == 0
                ? BlogContentResult<PostModel>.Failure("Content unavailable")
                : BlogContentResult<PostModel>.Success(CreatePost())));

        var cut = Render<Post>(parameters => parameters.Add(component => component.slug, "test-post"));
        cut.Find(".retry-button").Click();

        cut.WaitForAssertion(() =>
        {
            cut.FindAll(".content-error").Should().BeEmpty();
            cut.FindAll("article").Should().ContainSingle();
        });
    }

    private static PostModel CreatePost() => new()
    {
        Title = "Test post",
        Slug = "test-post",
        Description = "A short description",
        Image = "https://example.com/cover.png",
        Date = new DateTime(2024, 1, 1),
        Tags = ["dotnet"],
        Text = "**Body**",
    };
}
