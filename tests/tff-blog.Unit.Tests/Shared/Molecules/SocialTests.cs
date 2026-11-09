using AwesomeAssertions;
using Bunit;
using Microsoft.Extensions.DependencyInjection;
using Tff.Blog.Client.Configuration;
using Tff.Blog.Client.Shared.Molecules;

namespace Tff.Blog.Client.Unit.Tests.Shared.Molecules;

public class SocialTests : BunitContext
{
    public SocialTests()
    {
        Services.AddSingleton(new FeedLinkSettings("/rss.xml"));
    }

    [Fact]
    public void Social_RendersTheSocialLinksAndTheFeed()
    {
        var cut = Render<Social>();

        cut.FindAll("a").Should().HaveCount(4);
        cut.FindAll("img")
            .Select(icon => icon.GetAttribute("src"))
            .Should().ContainInOrder("icons/github.svg", "icons/linkedin.svg", "icons/x.svg", "icons/rss.svg");
    }

    [Fact]
    public void Social_RendersTheConfiguredFeedUrl()
    {
        var cut = Render<Social>();

        var link = cut.Find("a[href='/rss.xml']");

        link.GetAttribute("title").Should().Be("RSS feed");
        link.QuerySelector("img")!.GetAttribute("src").Should().Be("icons/rss.svg");
        link.QuerySelector("img")!.GetAttribute("alt").Should().Be("RSS feed icon");
    }

    [Fact]
    public void Social_UsesTheFeedUrlFromTheConfiguration()
    {
        Services.AddSingleton(new FeedLinkSettings("http://localhost:7071/api/rss.xml"));

        var cut = Render<Social>();

        cut.Find("a[href='http://localhost:7071/api/rss.xml']").Should().NotBeNull();
    }

    [Fact]
    public void Social_WithInvertColor_InvertsTheFeedIcon()
    {
        var cut = Render<Social>(parameters => parameters.Add(component => component.InvertColor, true));

        cut.Find("a[title='RSS feed'] img").GetAttribute("class").Should().Be("icon inverted");
    }
}
