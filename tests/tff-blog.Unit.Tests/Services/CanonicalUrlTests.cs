using AwesomeAssertions;
using Tff.Blog.Client.Services;

namespace Tff.Blog.Client.Unit.Tests.Services;

public class CanonicalUrlTests
{
    [Fact]
    public void From_KeepsTheRootUrl()
    {
        CanonicalUrl.From("https://www.tiagofonseca.dev/")
            .Should()
            .Be("https://www.tiagofonseca.dev/");
    }

    [Fact]
    public void From_TrimsTheTrailingSlash()
    {
        CanonicalUrl.From("https://www.tiagofonseca.dev/about/")
            .Should()
            .Be("https://www.tiagofonseca.dev/about");
    }

    [Fact]
    public void From_DropsTheQueryStringAndFragment()
    {
        CanonicalUrl.From("https://www.tiagofonseca.dev/post/hello-world/?preview=secret#section")
            .Should()
            .Be("https://www.tiagofonseca.dev/post/hello-world");
    }

    [Fact]
    public void From_KeepsTheRootWhenItHasAQueryString()
    {
        CanonicalUrl.From("https://www.tiagofonseca.dev/?preview=secret")
            .Should()
            .Be("https://www.tiagofonseca.dev/");
    }

    [Fact]
    public void From_KeepsANonDefaultPort()
    {
        CanonicalUrl.From("http://localhost:5000/post/hello-world/")
            .Should()
            .Be("http://localhost:5000/post/hello-world");
    }

    [Fact]
    public void From_AcceptsAUri()
    {
        CanonicalUrl.From(new Uri("https://www.tiagofonseca.dev/cookie-policy/"))
            .Should()
            .Be("https://www.tiagofonseca.dev/cookie-policy");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void From_WithABlankUrl_Throws(string url)
    {
        var act = () => CanonicalUrl.From(url);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void From_WithARelativeUrl_Throws()
    {
        // A rooted path ("/about") is treated as an absolute file URI on Unix, so use a
        // scheme-less relative URL that is rejected on every platform.
        var act = () => CanonicalUrl.From("about/page");

        act.Should().Throw<UriFormatException>();
    }
}
