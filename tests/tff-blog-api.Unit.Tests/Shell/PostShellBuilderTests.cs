using AwesomeAssertions;
using Tff.Blog.Api.Shell;
using Tff.Blog.Shared.Models;

namespace Tff.Blog.Api.Unit.Tests.Shell;

public class PostShellBuilderTests
{
    private const string SiteUrl = "https://www.tiagofonseca.dev";
    private const string Shell = "<!DOCTYPE html><html lang=\"en\"><head><title>Tiago Fonseca - Blog</title></head><body><app /></body></html>";

    [Fact]
    public void Build_InjectsThePostMetadataBeforeTheHeadCloses()
    {
        var result = PostShellBuilder.Build(Shell, CreatePost(), SiteUrl);

        result.Should().Contain("<meta property=\"og:title\" content=\"Test post\" />");
        result.Should().Contain("<meta property=\"og:description\" content=\"A short description\" />");
        result.Should().Contain("<meta name=\"description\" content=\"A short description\" />");
        result.Should().Contain("<meta name=\"twitter:title\" content=\"Test post\" />");
        result.Should().Contain("<meta name=\"twitter:description\" content=\"A short description\" />");
        result.Should().Contain("<meta name=\"twitter:card\" content=\"summary_large_image\" />");
        result.Should().Contain("<link rel=\"canonical\" href=\"https://www.tiagofonseca.dev/post/test-post\" />");

        result.IndexOf("og:title", StringComparison.Ordinal)
            .Should().BeLessThan(result.IndexOf("</head>", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_ReplacesTheDocumentTitle()
    {
        var result = PostShellBuilder.Build(Shell, CreatePost(), SiteUrl);

        result.Should().Contain("<title>Test post - Tiago Fonseca</title>");
        result.Should().NotContain("<title>Tiago Fonseca - Blog</title>");
        result.Should().Contain("</head>");
    }

    [Fact]
    public void Build_UsesTheAbsoluteCoverImage()
    {
        var result = PostShellBuilder.Build(Shell, CreatePost(image: "https://cdn.example.com/cover.png"), SiteUrl);

        result.Should().Contain("<meta property=\"og:image\" content=\"https://cdn.example.com/cover.png\" />");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Build_WithoutACoverImage_FallsBackToTheSiteLogo(string image)
    {
        var result = PostShellBuilder.Build(Shell, CreatePost(image: image), SiteUrl);

        result.Should().Contain($"{SiteUrl}/images/logo_icon.png");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Build_WithoutADescription_UsesTheTitle(string? description)
    {
        var result = PostShellBuilder.Build(Shell, CreatePost(description: description), SiteUrl);

        result.Should().Contain("<meta property=\"og:description\" content=\"Test post\" />");
    }

    [Fact]
    public void Build_TruncatesTheSocialDescriptionAndKeepsTheFullMetaDescription()
    {
        var description = new string('a', 190) + " supercalifragilistic tail";

        var result = PostShellBuilder.Build(Shell, CreatePost(description: description), SiteUrl);

        result.Should().Contain($"<meta name=\"description\" content=\"{description}\" />");
        result.Should().Contain($"<meta property=\"og:description\" content=\"{new string('a', 190)}…\" />");
        result.Should().Contain($"<meta name=\"twitter:description\" content=\"{new string('a', 190)}…\" />");
    }

    [Fact]
    public void Build_HtmlEncodesTheValues()
    {
        var post = CreatePost(title: "Tom & Jerry", description: "A \"quoted\" <summary>");

        var result = PostShellBuilder.Build(Shell, post, SiteUrl);

        result.Should().Contain("content=\"Tom &amp; Jerry\"");
        result.Should().Contain("content=\"A &quot;quoted&quot; &lt;summary&gt;\"");
        result.Should().NotContain("content=\"Tom & Jerry\"");
    }

    [Fact]
    public void Build_TrimsTrailingSlashesFromTheSiteUrl()
    {
        var result = PostShellBuilder.Build(Shell, CreatePost(), $"{SiteUrl}/");

        result.Should().Contain("href=\"https://www.tiagofonseca.dev/post/test-post\"");
    }

    [Fact]
    public void Build_EscapesTheSlug()
    {
        var result = PostShellBuilder.Build(Shell, CreatePost(slug: "hello world/next"), SiteUrl);

        result.Should().Contain("https://www.tiagofonseca.dev/post/hello%20world%2Fnext");
    }

    [Fact]
    public void Build_WithoutAHeadElement_ReturnsTheShellUnchanged()
    {
        const string shell = "<!DOCTYPE html><html><body><app /></body></html>";

        PostShellBuilder.Build(shell, CreatePost(), SiteUrl).Should().Be(shell);
    }

    [Fact]
    public void Build_WithoutATitle_StillInjectsTheMetadata()
    {
        const string shell = "<!DOCTYPE html><html><head></head><body></body></html>";

        var result = PostShellBuilder.Build(shell, CreatePost(), SiteUrl);

        result.Should().Contain("<meta property=\"og:title\" content=\"Test post\" />");
        result.Should().Contain("</head>");
    }

    [Fact]
    public void Build_WithAnEmptyShell_ReturnsItUnchanged()
    {
        PostShellBuilder.Build(string.Empty, CreatePost(), SiteUrl).Should().BeEmpty();
    }

    private static PostModel CreatePost(
        string slug = "test-post",
        string title = "Test post",
        string? description = "A short description",
        string image = "https://example.com/cover.png")
    {
        return new PostModel
        {
            Slug = slug,
            Title = title,
            Description = description!,
            Image = image,
        };
    }
}
