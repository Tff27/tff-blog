using AwesomeAssertions;
using Bunit;
using Tff.Blog.Client.Shared.Molecules;

namespace Tff.Blog.Client.Unit.Tests.Shared.Molecules;

public class SocialTests : BunitContext
{
    [Fact]
    public void Social_RendersTheSocialLinks()
    {
        var cut = Render<Social>();

        cut.FindAll("a").Should().HaveCount(3);
        cut.FindAll("img")
            .Select(icon => icon.GetAttribute("src"))
            .Should().ContainInOrder("icons/github.svg", "icons/linkedin.svg", "icons/x.svg");
    }

    [Fact]
    public void Social_WithoutShowRssFeed_DoesNotRenderTheFeedLink()
    {
        var cut = Render<Social>();

        cut.FindAll("a[href='/rss.xml']").Should().BeEmpty();
    }

    [Fact]
    public void Social_WithShowRssFeed_RendersTheFeedLink()
    {
        var cut = Render<Social>(parameters => parameters.Add(component => component.ShowRssFeed, true));

        var link = cut.Find("a[href='/rss.xml']");

        link.GetAttribute("title").Should().Be("RSS feed");
        link.QuerySelector("img")!.GetAttribute("src").Should().Be("icons/rss.svg");
        link.QuerySelector("img")!.GetAttribute("alt").Should().Be("RSS feed icon");
    }

    [Fact]
    public void Social_WithInvertColor_InvertsTheFeedIcon()
    {
        var cut = Render<Social>(parameters => parameters
            .Add(component => component.ShowRssFeed, true)
            .Add(component => component.InvertColor, true));

        cut.Find("a[href='/rss.xml'] img").GetAttribute("class").Should().Be("icon inverted");
    }
}
