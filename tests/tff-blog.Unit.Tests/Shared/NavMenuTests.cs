using AwesomeAssertions;
using Bunit;
using Tff.Blog.Client.Shared;

namespace Tff.Blog.Client.Unit.Tests.Shared;

public class NavMenuTests : BunitContext
{
    [Fact]
    public void NavMenu_StartsCollapsed()
    {
        var cut = RenderNavMenu();

        cut.Find(".navbar").ClassList.Should().NotContain("mobile");
        cut.FindAll(".collapse").Should().ContainSingle();
    }

    [Fact]
    public void NavMenu_TogglerExpandsTheMenu()
    {
        var cut = RenderNavMenu();

        cut.Find(".navbar-toggler").Click();

        cut.Find(".navbar").ClassList.Should().Contain("mobile");
        cut.FindAll(".collapse").Should().BeEmpty();
    }

    [Fact]
    public void NavMenu_TogglerTwiceCollapsesTheMenuAgain()
    {
        var cut = RenderNavMenu();

        cut.Find(".navbar-toggler").Click();
        cut.Find(".navbar-toggler").Click();

        cut.Find(".navbar").ClassList.Should().NotContain("mobile");
        cut.FindAll(".collapse").Should().ContainSingle();
    }

    [Fact]
    public void NavMenu_RendersHomeAndAboutLinks()
    {
        var cut = RenderNavMenu();

        var links = cut.FindAll(".navbar-menu a");

        links.Should().HaveCount(2);
        links.Select(link => link.TextContent.Trim()).Should().ContainInOrder("Home", "About");
    }

    private IRenderedComponent<NavMenu> RenderNavMenu()
    {
        return Render<NavMenu>((ComponentParameterCollectionBuilder<NavMenu> _) => { });
    }
}


