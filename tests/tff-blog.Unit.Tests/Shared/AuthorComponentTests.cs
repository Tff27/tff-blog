using AwesomeAssertions;
using Bunit;
using Ganss.Xss;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Tff.Blog.Client.Services;
using Tff.Blog.Client.Shared;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Client.Unit.Tests.Shared;

public class AuthorComponentTests : BunitContext
{
    private readonly IBlogContentService _contentService = Substitute.For<IBlogContentService>();

    public AuthorComponentTests()
    {
        Services.AddSingleton(_contentService);

        var sanitizer = new HtmlSanitizer();
        sanitizer.AllowedAttributes.Add("class");
        Services.AddSingleton<IHtmlSanitizer>(sanitizer);
    }

    [Fact]
    public void Author_RendersTheAuthorDetails()
    {
        _contentService.GetMarkdownPageAsync<AuthorModel>("author", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(BlogContentResult<AuthorModel>.Success(new AuthorModel
            {
                Name = "Tiago Fonseca",
                Photo = "https://example.com/photo.jpg",
                Text = "Author bio",
            })));

        var cut = Render<AuthorComponent>();

        cut.Find(".authorbar .name").TextContent.Should().Contain("Tiago Fonseca");
        cut.Find(".authorbar img").GetAttribute("src").Should().Be("https://example.com/photo.jpg");
    }

    [Fact]
    public void Author_WhenLoadingFails_HidesTheAuthorBlock()
    {
        _contentService.GetMarkdownPageAsync<AuthorModel>("author", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(BlogContentResult<AuthorModel>.Failure("Content unavailable")));

        var cut = Render<AuthorComponent>();

        cut.FindAll(".authorbar").Should().BeEmpty();
    }

    [Fact]
    public void Author_WhenAuthorIsMissing_HidesTheAuthorBlock()
    {
        _contentService.GetMarkdownPageAsync<AuthorModel>("author", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(BlogContentResult<AuthorModel>.NotFound()));

        var cut = Render<AuthorComponent>();

        cut.FindAll(".authorbar").Should().BeEmpty();
    }
}
